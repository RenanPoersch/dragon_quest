param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestShopMenuChecks')
)
$ErrorActionPreference = 'Stop'
$shopRoot = Split-Path -Parent $PSScriptRoot
$shopData = Join-Path $UnityEditorPath 'Data'
$shopDotnet = Join-Path $shopData 'NetCoreRuntime/dotnet.exe'
$shopCsc = Get-ChildItem -LiteralPath (Join-Path $shopData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$shopRefs = Get-ChildItem -LiteralPath (Join-Path $shopData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$shopRuntime = Get-ChildItem -LiteralPath (Join-Path $shopData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$shopRefDir = Get-ChildItem -LiteralPath (Join-Path $shopRefs.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$shopOutput = Join-Path $WorkDirectory 'ShopMenuChecks.dll'
$shopArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$shopOutput")
$shopArguments += Get-ChildItem -LiteralPath $shopRefDir.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($shopFolder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $shopArguments += Get-ChildItem -LiteralPath (Join-Path $shopRoot "Assets/Scripts/$shopFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
foreach ($shopSource in @('Assets/Scripts/Exploration/ExplorationProgress.cs', 'Assets/Editor/Tools/Fixtures/PartyFixture.cs','Assets/Scripts/Presentation/Battle/HeroKnockoutPresentation.cs','Assets/Scripts/Presentation/Battle/DefeatedEnemyPresentation.cs','Assets/Scripts/Presentation/Battle/UltimateSpriteTimeline.cs',
    'Tests/ShopMenuChecks.cs', 'Tests/ShopChecks.cs')) {
    $shopArguments += Join-Path $shopRoot $shopSource
}
$shopResponse = Join-Path $WorkDirectory 'ShopMenuChecks.rsp'
$shopArguments | Select-Object -Unique | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $shopResponse -Encoding utf8
& $shopDotnet $shopCsc.FullName "@$shopResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$shopRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$shopRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'ShopMenuChecks.runtimeconfig.json') -Encoding utf8
& $shopDotnet $shopOutput
exit $LASTEXITCODE
