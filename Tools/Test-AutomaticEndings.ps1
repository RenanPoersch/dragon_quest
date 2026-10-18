param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestAutomaticEndingChecks')
)
$ErrorActionPreference = 'Stop'
$endingRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $endingRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Runtime')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# A separate project leaves the open game Editor and the user's saves untouched.
$endingProject = Join-Path $WorkDirectory 'Unity'
foreach ($endingFolder in @('Assets/Editor', 'Assets/Plugins', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $endingProject $endingFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $WorkDirectory 'Runtime/DragonQuest.Scripts.dll') -Destination (Join-Path $endingProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $endingRoot 'Tests/AutomaticEndingUnityChecks.cs') -Destination (Join-Path $endingProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $endingRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $endingProject 'ProjectSettings')
$endingInput = Get-ChildItem -LiteralPath (Join-Path $endingRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $endingInput) { throw 'Importe o Input System na Unity antes dos testes de integracao.' }
@{dependencies=@{'com.unity.inputsystem'=('file:' + $endingInput.FullName.Replace('\', '/'))}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $endingProject 'Packages/manifest.json') -Encoding utf8
$endingLog = Join-Path $endingProject 'validation.log'
$endingProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $endingProject + '"'), '-executeMethod', 'AutomaticEndingUnityChecks.Run', '-logFile', ('"' + $endingLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Unity automatic ending checks PID $($endingProcess.Id). Log: $endingLog"
$endingProcess.WaitForExit()
Get-Content -LiteralPath $endingLog | Select-String 'AUTOMATIC_ENDINGS_UNITY_OK|Exception:|error CS|Assertion failed'
if ($endingProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $endingLog -Pattern 'AUTOMATIC_ENDINGS_UNITY_OK' -Quiet)) {
    throw "Validacao dos finais automaticos falhou. Consulte $endingLog"
}
