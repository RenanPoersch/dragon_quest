param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestSaveChecks')
)
$ErrorActionPreference = 'Stop'
$saveProjectRoot = Split-Path -Parent $PSScriptRoot
$saveUnityData = Join-Path $UnityEditorPath 'Data'
$saveDotnet = Join-Path $saveUnityData 'NetCoreRuntime/dotnet.exe'
$saveSdkRoot = Join-Path $saveUnityData 'DotNetSdk'
$saveCsc = Get-ChildItem -LiteralPath (Join-Path $saveSdkRoot 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$saveRefFolder = Get-ChildItem -LiteralPath (Join-Path $saveSdkRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$saveRuntimeFolder = Get-ChildItem -LiteralPath (Join-Path $saveUnityData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$saveReferenceDirectory = Get-ChildItem -LiteralPath (Join-Path $saveRefFolder.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$saveOutputFile = Join-Path $WorkDirectory 'SaveChecks.dll'
$saveCoreAssembly = Join-Path $saveUnityData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$saveArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$saveOutputFile", "-reference:$saveCoreAssembly")
$saveArguments += Get-ChildItem -LiteralPath $saveReferenceDirectory.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($saveFolder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression', 'Persistence')) {
    $saveArguments += Get-ChildItem -LiteralPath (Join-Path $saveProjectRoot "Assets/Scripts/$saveFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
$saveArguments += Join-Path $saveProjectRoot 'Assets/Scripts/Exploration/ExplorationProgress.cs'
$saveArguments += Join-Path $saveProjectRoot 'Assets/Editor/Tools/Fixtures/PartyFixture.cs'
$saveArguments += Join-Path $saveProjectRoot 'Assets/Scripts/Exploration/WorldNavigation.cs'
foreach ($saveName in @('WorldLayout', 'CityLayout', 'SecretLayout', 'RuinsLayout', 'VolcanoLayout', 'InnLayout',
    'QuarryLayout', 'EastForestLayout', 'CompactShopLayout', 'WeaponShopLayout', 'ThroneRoomLayout')) {
    $saveArguments += Join-Path $saveProjectRoot "Assets/Scripts/Exploration/Layouts/$saveName.cs"
}
$saveArguments += Join-Path $saveProjectRoot 'Assets/Scripts/Presentation/World/Rendering/ObliqueProjection.cs'
$saveArguments += Join-Path $saveProjectRoot 'Tests/SaveChecks.cs'
$saveResponse = Join-Path $WorkDirectory 'SaveChecks.rsp'
$saveArguments | Select-Object -Unique | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $saveResponse -Encoding utf8
& $saveDotnet $saveCsc.FullName "@$saveResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $saveCoreAssembly -Destination $WorkDirectory -Force
$saveRuntimeConfig = @{ runtimeOptions = @{ tfm = $saveReferenceDirectory.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $saveRuntimeFolder.Name } } }
$saveRuntimeConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'SaveChecks.runtimeconfig.json') -Encoding utf8
& $saveDotnet $saveOutputFile $WorkDirectory
exit $LASTEXITCODE
