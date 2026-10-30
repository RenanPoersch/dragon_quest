param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestMateriaMenuChecks')
)
$ErrorActionPreference = 'Stop'
$materiaRoot = Split-Path -Parent $PSScriptRoot
$materiaData = Join-Path $UnityEditorPath 'Data'
$materiaDotnet = Join-Path $materiaData 'NetCoreRuntime/dotnet.exe'
$materiaCsc = Get-ChildItem -LiteralPath (Join-Path $materiaData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$materiaRefs = Get-ChildItem -LiteralPath (Join-Path $materiaData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$materiaRuntime = Get-ChildItem -LiteralPath (Join-Path $materiaData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$materiaRefDir = Get-ChildItem -LiteralPath (Join-Path $materiaRefs.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$materiaOutput = Join-Path $WorkDirectory 'MateriaMenuChecks.dll'
$materiaArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$materiaOutput")
$materiaArguments += Get-ChildItem -LiteralPath $materiaRefDir.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($materiaFolder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $materiaArguments += Get-ChildItem -LiteralPath (Join-Path $materiaRoot "Assets/Scripts/$materiaFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
foreach ($materiaSource in @('Assets/Scripts/Exploration/ExplorationProgress.cs', 'Assets/Scripts/Prototype/PrototypeBattleFactory.cs',
    'Assets/Scripts/Prototype/PrototypeCastleBattleFactory.cs', 'Assets/Scripts/Prototype/PrototypePartyFactory.cs',
    'Assets/Scripts/Prototype/PrototypeShopFactory.cs', 'Tests/MateriaMenuChecks.cs')) {
    $materiaArguments += Join-Path $materiaRoot $materiaSource
}
$materiaResponse = Join-Path $WorkDirectory 'MateriaMenuChecks.rsp'
$materiaArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $materiaResponse -Encoding utf8
& $materiaDotnet $materiaCsc.FullName "@$materiaResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$materiaRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$materiaRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'MateriaMenuChecks.runtimeconfig.json') -Encoding utf8
& $materiaDotnet $materiaOutput
exit $LASTEXITCODE
