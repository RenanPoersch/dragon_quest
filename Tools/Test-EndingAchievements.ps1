param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestAchievementChecks'),
    [switch]$UnityIntegration
)
$ErrorActionPreference = 'Stop'
$achievementRoot = Split-Path -Parent $PSScriptRoot
$achievementData = Join-Path $UnityEditorPath 'Data'
$achievementDotnet = Join-Path $achievementData 'NetCoreRuntime/dotnet.exe'
$achievementCsc = Get-ChildItem -LiteralPath (Join-Path $achievementData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$achievementPack = Get-ChildItem -LiteralPath (Join-Path $achievementData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$achievementRefs = Get-ChildItem -LiteralPath (Join-Path $achievementPack.FullName 'ref') -Directory | Select-Object -First 1
$achievementRuntime = Get-ChildItem -LiteralPath (Join-Path $achievementData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$achievementOutput = Join-Path $WorkDirectory 'EndingAchievementChecks.dll'
$achievementArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$achievementOutput")
$achievementArguments += Get-ChildItem -LiteralPath $achievementRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($achievementFolder in @('Combat', 'Equipment', 'Inventory', 'Progression', 'Shops')) {
    $achievementArguments += Get-ChildItem -LiteralPath (Join-Path $achievementRoot "Assets/Scripts/$achievementFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
$achievementArguments += Join-Path $achievementRoot 'Assets/Scripts/Exploration/ExplorationProgress.cs'
$achievementArguments += Join-Path $achievementRoot 'Assets/Scripts/Persistence/EndingAchievementStore.cs'
$achievementArguments += Join-Path $achievementRoot 'Tests/EndingAchievementChecks.cs'
$achievementResponse = Join-Path $WorkDirectory 'EndingAchievementChecks.rsp'
$achievementArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $achievementResponse -Encoding utf8
& $achievementDotnet $achievementCsc.FullName "@$achievementResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$achievementRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$achievementRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'EndingAchievementChecks.runtimeconfig.json') -Encoding utf8
& $achievementDotnet $achievementOutput $WorkDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not $UnityIntegration) { exit 0 }

# Validate imported artwork in isolation; the user's open Editor keeps running.
$achievementProject = Join-Path $WorkDirectory 'Unity'
foreach ($achievementFolder in @('Assets/Editor', 'Assets/Plugins', 'Assets/Resources/PixelArt', 'Assets/Scripts/Prototype', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $achievementProject $achievementFolder) -Force | Out-Null
}
$achievementRuntimeOutput = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $achievementRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $achievementRuntimeOutput
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $achievementRuntimeOutput 'DragonQuest.Scripts.dll') -Destination (Join-Path $achievementProject 'Assets/Plugins') -Force
Copy-Item -LiteralPath (Join-Path $achievementRoot 'Assets/Editor/EndingAchievementValidation.cs') -Destination (Join-Path $achievementProject 'Assets/Editor') -Force
Copy-Item -LiteralPath (Join-Path $achievementRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $achievementProject 'ProjectSettings') -Force
foreach ($achievementName in @('PrototypeSaveController', 'PrototypeEndingController')) {
    # Read by the validator, but excluded from compilation because they are already in the DLL.
    $achievementSourceTarget = Join-Path $achievementProject "Assets/Scripts/Prototype/$achievementName.cs"
    Copy-Item -LiteralPath (Join-Path $achievementRoot "Assets/Scripts/Prototype/$achievementName.cs") -Destination $achievementSourceTarget -Force
}
# The source copies are textual evidence only, excluded by their own assembly definition.
'{"name":"AchievementSourceEvidence","includePlatforms":["Editor"],"defineConstraints":["ACHIEVEMENT_EVIDENCE_ONLY"]}' |
    Set-Content -LiteralPath (Join-Path $achievementProject 'Assets/Scripts/Prototype/Evidence.asmdef') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $achievementRoot 'Assets/Resources/Fonts') -Destination (Join-Path $achievementProject 'Assets/Resources') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $achievementRoot 'Assets/Resources/PixelArt/Achievements') -Destination (Join-Path $achievementProject 'Assets/Resources/PixelArt') -Recurse -Force
foreach ($achievementFile in @('DragonScale.png', 'DragonScale.png.meta')) {
    Copy-Item -LiteralPath (Join-Path $achievementRoot "Assets/Resources/PixelArt/$achievementFile") -Destination (Join-Path $achievementProject 'Assets/Resources/PixelArt') -Force
}
$achievementPackage = Get-ChildItem -LiteralPath (Join-Path $achievementRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $achievementPackage) { throw 'Importe o Input System na Unity antes dos testes de integracao.' }
@{dependencies=@{'com.unity.inputsystem'=('file:' + $achievementPackage.FullName.Replace('\', '/'))}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $achievementProject 'Packages/manifest.json') -Encoding utf8
$achievementLog = Join-Path $achievementProject 'validation.log'
$achievementProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $achievementProject + '"'), '-executeMethod', 'EndingAchievementValidation.Run', '-logFile', ('"' + $achievementLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Unity achievement validation PID $($achievementProcess.Id). Log: $achievementLog"
$achievementProcess.WaitForExit()
Get-Content -LiteralPath $achievementLog | Select-String 'ENDING_ACHIEVEMENT_UNITY_OK|^Exception:|error CS|InvalidOperationException'
if ($achievementProcess.ExitCode -ne 0) { throw "Unity achievement validation failed. See $achievementLog" }
if (-not (Select-String -LiteralPath $achievementLog -Pattern 'ENDING_ACHIEVEMENT_UNITY_OK' -Quiet)) { throw "Unity did not confirm achievement validation. See $achievementLog" }
