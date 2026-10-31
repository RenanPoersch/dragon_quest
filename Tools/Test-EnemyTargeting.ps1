param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestEnemyTargetingChecks')
)

$ErrorActionPreference = 'Stop'
$targetProjectRoot = Split-Path -Parent $PSScriptRoot
$targetUnityData = Join-Path $UnityEditorPath 'Data'
$targetDotnet = Join-Path $targetUnityData 'NetCoreRuntime/dotnet.exe'
$targetCsc = Get-ChildItem (Join-Path $targetUnityData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$targetPack = Get-ChildItem (Join-Path $targetUnityData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$targetRefs = Get-ChildItem (Join-Path $targetPack.FullName 'ref') -Directory | Select-Object -First 1
$targetRuntime = Get-ChildItem (Join-Path $targetUnityData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$targetOutput = Join-Path $WorkDirectory 'EnemyTargetingChecks.dll'
$targetArgs = @('-nologo', '-target:exe', '-nostdlib+', "-out:$targetOutput")
$targetArgs += Get-ChildItem $targetRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($folder in @('Combat','Content', 'Equipment', 'Inventory', 'Shops', 'Progression')) {
    $targetArgs += Get-ChildItem (Join-Path $targetProjectRoot "Assets/Scripts/$folder") -Filter '*.cs' | ForEach-Object { $_.FullName }
}
$targetArgs += Join-Path $targetProjectRoot 'Assets/Scripts/Exploration/ExplorationProgress.cs'
$targetArgs += Join-Path $targetProjectRoot 'Tests/EnemyTargetingChecks.cs'
$targetResponse = Join-Path $WorkDirectory 'EnemyTargetingChecks.rsp'
$targetArgs | Select-Object -Unique | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $targetResponse -Encoding utf8
& $targetDotnet $targetCsc.FullName "@$targetResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{ runtimeOptions = @{ tfm = $targetRefs.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $targetRuntime.Name } } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'EnemyTargetingChecks.runtimeconfig.json') -Encoding utf8
& $targetDotnet $targetOutput
exit $LASTEXITCODE
