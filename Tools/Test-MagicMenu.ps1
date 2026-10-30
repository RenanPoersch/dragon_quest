param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestMagicMenuChecks')
)
$ErrorActionPreference = 'Stop'
$magicRoot = Split-Path -Parent $PSScriptRoot
$magicData = Join-Path $UnityEditorPath 'Data'
$magicDotnet = Join-Path $magicData 'NetCoreRuntime/dotnet.exe'
$magicCsc = Get-ChildItem -LiteralPath (Join-Path $magicData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$magicRefs = Get-ChildItem -LiteralPath (Join-Path $magicData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$magicRuntime = Get-ChildItem -LiteralPath (Join-Path $magicData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$magicRefDir = Get-ChildItem -LiteralPath (Join-Path $magicRefs.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$magicOutput = Join-Path $WorkDirectory 'MagicMenuChecks.dll'
$magicArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$magicOutput")
$magicArguments += Get-ChildItem -LiteralPath $magicRefDir.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($magicFolder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $magicArguments += Get-ChildItem -LiteralPath (Join-Path $magicRoot "Assets/Scripts/$magicFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
foreach ($magicSource in @('Assets/Scripts/Exploration/ExplorationProgress.cs', 'Assets/Scripts/Prototype/PrototypeBattleFactory.cs',
    'Assets/Scripts/Prototype/PrototypeCastleBattleFactory.cs', 'Assets/Scripts/Prototype/PrototypePartyFactory.cs',
    'Assets/Scripts/Prototype/PrototypeShopFactory.cs', 'Tests/MagicMenuChecks.cs')) {
    $magicArguments += Join-Path $magicRoot $magicSource
}
$magicResponse = Join-Path $WorkDirectory 'MagicMenuChecks.rsp'
$magicArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $magicResponse -Encoding utf8
& $magicDotnet $magicCsc.FullName "@$magicResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$magicRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$magicRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'MagicMenuChecks.runtimeconfig.json') -Encoding utf8
& $magicDotnet $magicOutput
exit $LASTEXITCODE
