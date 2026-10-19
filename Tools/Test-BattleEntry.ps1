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
Copy-Item -LiteralPath (Join-Path $entryRoot 'Tests/BattleEntryPlayModeChecks.cs') -Destination (Join-Path $entryProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $entryRoot 'Assets/Resources/BattleEntrySwirl.shader') -Destination (Join-Path $entryProject 'Assets/Resources')
foreach ($entryExtension in @('.png', '.png.meta')) {
    Copy-Item -LiteralPath (Join-Path $entryRoot "Assets/Resources/PixelArt/WorldGroundMap$entryExtension") -Destination (Join-Path $entryProject 'Assets/Resources/PixelArt')
}
foreach ($entryResource in @('ThroneRoomInterior.png', 'ThroneRoomInterior.png.meta')) {
    Copy-Item -LiteralPath (Join-Path $entryRoot "Assets/Resources/PixelArt/$entryResource") -Destination (Join-Path $entryProject 'Assets/Resources/PixelArt')
}
foreach ($entryShader in @('ForestPixelBackdrop.shader', 'PixelCharacter.shader')) {
    Copy-Item -LiteralPath (Join-Path $entryRoot "Assets/Resources/$entryShader") -Destination (Join-Path $entryProject 'Assets/Resources')
}
Copy-Item -LiteralPath (Join-Path $entryRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $entryProject 'ProjectSettings')
# Exercise camera capture under the same URP 2D pipeline as the game.
Copy-Item -LiteralPath (Join-Path $entryRoot 'Assets/Settings') -Destination (Join-Path $entryProject 'Assets') -Recurse -Force
foreach ($entrySetting in @('GraphicsSettings.asset', 'QualitySettings.asset', 'ProjectSettings.asset')) {
    Copy-Item -LiteralPath (Join-Path $entryRoot "ProjectSettings/$entrySetting") -Destination (Join-Path $entryProject 'ProjectSettings') -Force
}
$entryDependencies = @{ 'com.unity.modules.physics2d' = '1.0.0' }
$entryQueue = [Collections.Generic.Queue[string]]::new()
$entryQueue.Enqueue('com.unity.inputsystem')
$entryQueue.Enqueue('com.unity.render-pipelines.universal')
while ($entryQueue.Count -gt 0) {
    $entryName = $entryQueue.Dequeue()
    if ($entryDependencies.ContainsKey($entryName)) { continue }
    if ($entryName.StartsWith('com.unity.modules.')) { $entryDependencies[$entryName] = '1.0.0'; continue }
    $entryCached = Get-ChildItem -LiteralPath (Join-Path $entryRoot 'Library/PackageCache') -Directory -Filter "$entryName@*" | Select-Object -First 1
    if (-not $entryCached) { throw "Importe $entryName na Unity antes dos testes." }
    $entryDependencies[$entryName] = 'file:' + $entryCached.FullName.Replace('\', '/')
    $entryPackage = Get-Content -LiteralPath (Join-Path $entryCached.FullName 'package.json') -Raw | ConvertFrom-Json
    if ($entryPackage.dependencies) {
        foreach ($entryDependency in $entryPackage.dependencies.PSObject.Properties.Name) { $entryQueue.Enqueue($entryDependency) }
    }
}
@{ dependencies = $entryDependencies } | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $entryProject 'Packages/manifest.json') -Encoding utf8
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
    $entryPlayLog = Join-Path $entryProject 'playmode.log'
    $entryPlayProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-force-d3d11',
        '-projectPath', ('"' + $entryProject + '"'), '-executeMethod', 'BattleEntryPlayModeChecks.Run', '-logFile', ('"' + $entryPlayLog + '"')) -WindowStyle Hidden -PassThru
    Write-Output "Battle entry Play Mode PID $($entryPlayProcess.Id). Log: $entryPlayLog"
    $entryPlayProcess.WaitForExit()
    Get-Content -LiteralPath $entryPlayLog | Select-String 'BATTLE_ENTRY_PLAYMODE_CHECKS_OK|Exception:|error CS|Shader error'
    if ($entryPlayProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $entryPlayLog -Pattern 'BATTLE_ENTRY_PLAYMODE_CHECKS_OK' -Quiet)) {
        throw "Validacao da transicao em execucao falhou. Consulte $entryPlayLog"
    }
}
finally { $env:DQ_BATTLE_ENTRY_OUTPUT = $entryPreviousOutput }
