param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestEndingTransitionChecks')
)
$ErrorActionPreference = 'Stop'
$transitionRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $transitionRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Runtime')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$transitionProject = Join-Path $WorkDirectory 'Unity'
foreach ($transitionFolder in @('Assets/Editor', 'Assets/Plugins', 'Assets/Resources/PixelArt', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $transitionProject $transitionFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $WorkDirectory 'Runtime/DragonQuest.Scripts.dll') -Destination (Join-Path $transitionProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $transitionRoot 'Tests/EndingTransitionUnityChecks.cs') -Destination (Join-Path $transitionProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $transitionRoot 'Assets/Resources/PixelArt/Endings') -Destination (Join-Path $transitionProject 'Assets/Resources/PixelArt') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $transitionRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $transitionProject 'ProjectSettings')
$transitionInput = Get-ChildItem -LiteralPath (Join-Path $transitionRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $transitionInput) { throw 'Importe o Input System na Unity antes dos testes.' }
@{ dependencies = @{
    'com.unity.inputsystem' = ('file:' + $transitionInput.FullName.Replace('\', '/'))
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.physics2d' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
} } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $transitionProject 'Packages/manifest.json') -Encoding utf8
$transitionLog = Join-Path $transitionProject 'validation.log'
$transitionProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-force-d3d11',
    '-projectPath', ('"' + $transitionProject + '"'), '-executeMethod', 'EndingTransitionUnityChecks.Run',
    '-logFile', ('"' + $transitionLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Ending transition checks PID $($transitionProcess.Id). Log: $transitionLog"
$transitionProcess.WaitForExit()
Get-Content -LiteralPath $transitionLog | Select-String 'ENDING_TRANSITIONS_UNITY_OK|Exception:|error CS|Shader error'
if ($transitionProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $transitionLog -Pattern 'ENDING_TRANSITIONS_UNITY_OK' -Quiet)) {
    throw "Validacao das transicoes dos finais falhou. Consulte $transitionLog"
}
