param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestItemMenuChecks')
)
$ErrorActionPreference = 'Stop'
$itemRoot = Split-Path -Parent $PSScriptRoot
$itemData = Join-Path $UnityEditorPath 'Data'
$itemDotnet = Join-Path $itemData 'NetCoreRuntime/dotnet.exe'
$itemCsc = Get-ChildItem -LiteralPath (Join-Path $itemData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$itemRefs = Get-ChildItem -LiteralPath (Join-Path $itemData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$itemRuntime = Get-ChildItem -LiteralPath (Join-Path $itemData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$itemRefDir = Get-ChildItem -LiteralPath (Join-Path $itemRefs.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$itemOutput = Join-Path $WorkDirectory 'ItemMenuChecks.dll'
$itemArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$itemOutput")
$itemArguments += Get-ChildItem -LiteralPath $itemRefDir.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($itemFolder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $itemArguments += Get-ChildItem -LiteralPath (Join-Path $itemRoot "Assets/Scripts/$itemFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
foreach ($itemSource in @('Assets/Scripts/Exploration/ExplorationProgress.cs', 'Assets/Scripts/Prototype/PrototypeBattleFactory.cs',
    'Assets/Scripts/Prototype/PrototypeCastleBattleFactory.cs', 'Assets/Scripts/Prototype/PrototypePartyFactory.cs',
    'Assets/Scripts/Prototype/PrototypeShopFactory.cs', 'Tests/ItemMenuChecks.cs')) {
    $itemArguments += Join-Path $itemRoot $itemSource
}
$itemResponse = Join-Path $WorkDirectory 'ItemMenuChecks.rsp'
$itemArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $itemResponse -Encoding utf8
& $itemDotnet $itemCsc.FullName "@$itemResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$itemRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$itemRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'ItemMenuChecks.runtimeconfig.json') -Encoding utf8
& $itemDotnet $itemOutput
exit $LASTEXITCODE
