param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestEquipmentMenuChecks')
)
$ErrorActionPreference = 'Stop'
$equipmentRoot = Split-Path -Parent $PSScriptRoot
$equipmentData = Join-Path $UnityEditorPath 'Data'
$equipmentDotnet = Join-Path $equipmentData 'NetCoreRuntime/dotnet.exe'
$equipmentCsc = Get-ChildItem -LiteralPath (Join-Path $equipmentData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$equipmentRefs = Get-ChildItem -LiteralPath (Join-Path $equipmentData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$equipmentRuntime = Get-ChildItem -LiteralPath (Join-Path $equipmentData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$equipmentRefDir = Get-ChildItem -LiteralPath (Join-Path $equipmentRefs.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$equipmentOutput = Join-Path $WorkDirectory 'EquipmentMenuChecks.dll'
$equipmentArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$equipmentOutput")
$equipmentArguments += Get-ChildItem -LiteralPath $equipmentRefDir.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($equipmentFolder in @('Combat', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $equipmentArguments += Get-ChildItem -LiteralPath (Join-Path $equipmentRoot "Assets/Scripts/$equipmentFolder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
foreach ($equipmentSource in @('Assets/Scripts/Exploration/ExplorationProgress.cs', 'Assets/Scripts/Prototype/PrototypeBattleFactory.cs',
    'Assets/Scripts/Prototype/PrototypeCastleBattleFactory.cs', 'Assets/Scripts/Prototype/PrototypePartyFactory.cs',
    'Assets/Scripts/Prototype/PrototypeShopFactory.cs', 'Tests/EquipmentMenuChecks.cs')) {
    $equipmentArguments += Join-Path $equipmentRoot $equipmentSource
}
$equipmentResponse = Join-Path $WorkDirectory 'EquipmentMenuChecks.rsp'
$equipmentArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $equipmentResponse -Encoding utf8
& $equipmentDotnet $equipmentCsc.FullName "@$equipmentResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$equipmentRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$equipmentRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'EquipmentMenuChecks.runtimeconfig.json') -Encoding utf8
& $equipmentDotnet $equipmentOutput
exit $LASTEXITCODE
