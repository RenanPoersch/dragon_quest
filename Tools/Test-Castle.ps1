param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestCastleChecks')
)
$ErrorActionPreference = 'Stop'
$castleRoot = Split-Path -Parent $PSScriptRoot
$castleData = Join-Path $UnityEditorPath 'Data'
$castleDotnet = Join-Path $castleData 'NetCoreRuntime/dotnet.exe'
$castleCsc = Get-ChildItem (Join-Path $castleData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$castlePack = Get-ChildItem (Join-Path $castleData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$castleRefs = Get-ChildItem (Join-Path $castlePack.FullName 'ref') -Directory | Select-Object -First 1
$castleRuntime = Get-ChildItem (Join-Path $castleData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$castleOutput = Join-Path $WorkDirectory 'CastleChecks.dll'
$castleArgs = @('-nologo','-target:exe','-nostdlib+',('-out:"'+$castleOutput+'"'))
$castleArgs += Get-ChildItem $castleRefs.FullName -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
foreach ($folder in @('Combat','Content','Equipment','Inventory','Shops','Progression')) {
    $castleArgs += Get-ChildItem (Join-Path $castleRoot "Assets/Scripts/$folder") -Filter '*.cs' | ForEach-Object { '"'+$_.FullName+'"' }
}
foreach ($relative in @('Assets/Scripts/Exploration/ExplorationProgress.cs',
    'Assets/Scripts/Prototype/PrototypeBattleFactory.cs','Assets/Scripts/Prototype/PrototypeCastleBattleFactory.cs',
    'Assets/Scripts/Prototype/PrototypePartyFactory.cs','Assets/Scripts/Prototype/PrototypeShopFactory.cs',
    'Assets/Scripts/Prototype/BattleAnimationCatalog.cs','Assets/Scripts/Prototype/CastleMeteorPresentation.cs',
    'Tests/CastleChecks.cs','Tests/CastlePresentationChecks.cs')) { $castleArgs += '"'+(Join-Path $castleRoot $relative)+'"' }
$castleRsp = Join-Path $WorkDirectory 'CastleChecks.rsp'
$castleArgs | Set-Content -LiteralPath $castleRsp -Encoding utf8
& $castleDotnet $castleCsc.FullName ('@'+$castleRsp)
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$castleRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$castleRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $WorkDirectory 'CastleChecks.runtimeconfig.json') -Encoding utf8
& $castleDotnet $castleOutput
exit $LASTEXITCODE
