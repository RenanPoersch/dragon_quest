param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestEndingMotionChecks'),
    [switch]$LinearGoodEnding
)
$ErrorActionPreference = 'Stop'
$motionRoot = Split-Path -Parent $PSScriptRoot
foreach ($motionSuite in @('Test-FinalOne.ps1','Test-GoodEnding.ps1','Test-HeroPossessed.ps1','Test-CastleImpact.ps1','Test-UnityScripts.ps1')) {
    & (Join-Path $motionRoot "Tools/$motionSuite") -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory $motionSuite.Replace('.ps1',''))
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# An isolated project validates actual GPU frames without changing the open project or saves.
$motionProjectName = if ($LinearGoodEnding) { 'Unity-LinearGoodEnding' } else { 'Unity' }
$motionProject = Join-Path $WorkDirectory $motionProjectName
foreach ($motionFolder in @('Assets/Editor','Assets/Plugins','Assets/Resources/PixelArt/Endings','Assets/Resources/Fonts','Packages','ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $motionProject $motionFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $WorkDirectory 'Test-UnityScripts/DragonQuest.Scripts.dll') -Destination (Join-Path $motionProject 'Assets/Plugins')
foreach ($motionValidator in @('EndingMotionValidation.cs','FinalOneValidation.cs','KingFreedValidation.cs','HeroPossessedValidation.cs','CastleImpactValidation.cs')) {
    Copy-Item -LiteralPath (Join-Path $motionRoot "Assets/Editor/$motionValidator") -Destination (Join-Path $motionProject 'Assets/Editor')
}
Get-ChildItem -LiteralPath (Join-Path $motionRoot 'Assets/Resources/PixelArt/Endings') -File |
    Copy-Item -Destination (Join-Path $motionProject 'Assets/Resources/PixelArt/Endings')
Get-ChildItem -LiteralPath (Join-Path $motionRoot 'Assets/Resources/Fonts') -File |
    Copy-Item -Destination (Join-Path $motionProject 'Assets/Resources/Fonts')
Copy-Item -LiteralPath (Join-Path $motionRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $motionProject 'ProjectSettings')
# The legacy GPU checks use Gamma; the good-ending color regression also runs in the game's Linear space.
if ($LinearGoodEnding) {
    Copy-Item -LiteralPath (Join-Path $motionRoot 'ProjectSettings/ProjectSettings.asset') -Destination (Join-Path $motionProject 'ProjectSettings')
}
$motionInput = Get-ChildItem -LiteralPath (Join-Path $motionRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $motionInput) { throw 'Importe o Input System na Unity antes da validacao.' }
@{dependencies=@{'com.unity.inputsystem'=('file:'+$motionInput.FullName.Replace('\','/'))}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $motionProject 'Packages/manifest.json') -Encoding utf8
$motionLog = Join-Path $motionProject 'validation.log'
$motionCaptures = Join-Path $WorkDirectory 'Captures'
$motionMethod = if ($LinearGoodEnding) { 'KingFreedValidation.Run' } else { 'EndingMotionValidation.RunBatch' }
$motionMarker = if ($LinearGoodEnding) { 'GOOD_ENDING_VALIDATION_OK' } else { 'ENDING_MOTION_OK' }
$motionProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode','-force-d3d11','-quit',
    '-projectPath',('"'+$motionProject+'"'),'-executeMethod',$motionMethod,
    '-logFile',('"'+$motionLog+'"'),'-endingMotionCaptureDir',('"'+$motionCaptures+'"')) -WindowStyle Hidden -PassThru
Write-Output "Ending motion GPU checks PID $($motionProcess.Id). Log: $motionLog"
$motionProcess.WaitForExit()
Get-Content -LiteralPath $motionLog | Select-String 'ENDING_MOTION_OK|VALIDATION_OK|Exception:|error CS|Shader error'
if ($motionProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $motionLog -Pattern $motionMarker -Quiet)) {
    throw "Validacao do movimento dos finais falhou. Consulte $motionLog"
}
