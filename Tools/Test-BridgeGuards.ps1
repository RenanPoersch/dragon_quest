param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestBridgeGuardChecks')
)
$ErrorActionPreference = 'Stop'
$guardRoot = Split-Path -Parent $PSScriptRoot
$guardRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $guardRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $guardRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$guardProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
foreach ($guardFolder in @('Assets/Plugins', 'Assets/Editor', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $guardProject $guardFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $guardRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $guardProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $guardRoot 'Tests/BridgeGuardUnityChecks.cs') -Destination (Join-Path $guardProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $guardRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $guardProject 'ProjectSettings')
$guardInput = Get-ChildItem -LiteralPath (Join-Path $guardRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $guardInput) { throw 'Importe o Input System na Unity antes dos testes.' }
@{ dependencies = @{ 'com.unity.inputsystem' = ('file:' + $guardInput.FullName.Replace('\', '/')); 'com.unity.modules.physics2d' = '1.0.0' } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $guardProject 'Packages/manifest.json') -Encoding utf8
$guardLog = Join-Path $guardProject 'validation.log'
$guardProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics',
    '-projectPath', ('"' + $guardProject + '"'), '-executeMethod', 'BridgeGuardUnityChecks.Run', '-logFile', ('"' + $guardLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Bridge guard Unity PID $($guardProcess.Id). Log: $guardLog"
$guardProcess.WaitForExit()
Get-Content -LiteralPath $guardLog | Select-String 'BRIDGE_GUARD_UNITY_CHECKS_OK|Exception:|error CS'
if ($guardProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $guardLog -Pattern 'BRIDGE_GUARD_UNITY_CHECKS_OK' -Quiet)) {
    throw "Validacao da patrulha falhou. Consulte $guardLog"
}
