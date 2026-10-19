param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestBattleEntryChecks')
)
$ErrorActionPreference = 'Stop'
$entryRoot = Split-Path -Parent $PSScriptRoot
$entryRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $entryRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $entryRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$entryProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
foreach ($entryFolder in @('Assets/Plugins', 'Assets/Editor', 'Assets/Resources/PixelArt', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $entryProject $entryFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $entryRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $entryProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $entryRoot 'Tests/BattleEntryUnityChecks.cs') -Destination (Join-Path $entryProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $entryRoot 'Assets/Resources/BattleEntrySwirl.shader') -Destination (Join-Path $entryProject 'Assets/Resources')
foreach ($entryExtension in @('.png', '.png.meta')) {
    Copy-Item -LiteralPath (Join-Path $entryRoot "Assets/Resources/PixelArt/WorldGroundMap$entryExtension") -Destination (Join-Path $entryProject 'Assets/Resources/PixelArt')
}
Copy-Item -LiteralPath (Join-Path $entryRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $entryProject 'ProjectSettings')
$entryInput = Get-ChildItem -LiteralPath (Join-Path $entryRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $entryInput) { throw 'Importe o Input System na Unity antes dos testes.' }
@{ dependencies = @{ 'com.unity.inputsystem' = ('file:' + $entryInput.FullName.Replace('\', '/')); 'com.unity.modules.physics2d' = '1.0.0' } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $entryProject 'Packages/manifest.json') -Encoding utf8
$entryLog = Join-Path $entryProject 'validation.log'
$entryPreviousOutput = $env:DQ_BATTLE_ENTRY_OUTPUT
try {
    $env:DQ_BATTLE_ENTRY_OUTPUT = Join-Path $WorkDirectory 'Preview'
    $entryProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-force-d3d11',
        '-projectPath', ('"' + $entryProject + '"'), '-executeMethod', 'BattleEntryUnityChecks.Run', '-logFile', ('"' + $entryLog + '"')) -WindowStyle Hidden -PassThru
    Write-Output "Battle entry Unity PID $($entryProcess.Id). Log: $entryLog"
    $entryProcess.WaitForExit()
    Get-Content -LiteralPath $entryLog | Select-String 'BATTLE_ENTRY_UNITY_CHECKS_OK|Exception:|error CS|Shader error'
    if ($entryProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $entryLog -Pattern 'BATTLE_ENTRY_UNITY_CHECKS_OK' -Quiet)) {
        throw "Validacao da transicao falhou. Consulte $entryLog"
    }
}
finally { $env:DQ_BATTLE_ENTRY_OUTPUT = $entryPreviousOutput }
