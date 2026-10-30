param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestEndingTransitionTiming')
)
$ErrorActionPreference = 'Stop'
$timingRoot = Split-Path -Parent $PSScriptRoot
$timingData = Join-Path $UnityEditorPath 'Data'
$timingDotnet = Join-Path $timingData 'NetCoreRuntime/dotnet.exe'
$timingCsc = Get-ChildItem -LiteralPath (Join-Path $timingData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$timingPack = Get-ChildItem -LiteralPath (Join-Path $timingData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$timingRefs = Get-ChildItem -LiteralPath (Join-Path $timingPack.FullName 'ref') -Directory | Select-Object -First 1
$timingRuntime = Get-ChildItem -LiteralPath (Join-Path $timingData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$timingOutput = Join-Path $WorkDirectory 'EndingTransitionChecks.dll'
$timingArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$timingOutput")
$timingArguments += Get-ChildItem -LiteralPath $timingRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
foreach ($timingSource in @('BattleExitSequence', 'KingFreedSequence', 'HeroPossessedSequence', 'CastleImpactSequence', 'CastleDestroyedSequence')) {
    $timingArguments += Join-Path $timingRoot "Assets/Scripts/Presentation/Cinematics/Endings/$timingSource.cs"
}
$timingArguments += Join-Path $timingRoot 'Tests/EndingTransitionChecks.cs'
$timingResponse = Join-Path $WorkDirectory 'EndingTransitionChecks.rsp'
$timingArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $timingResponse -Encoding utf8
& $timingDotnet $timingCsc.FullName "@$timingResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$timingRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$timingRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'EndingTransitionChecks.runtimeconfig.json') -Encoding utf8
& $timingDotnet $timingOutput
exit $LASTEXITCODE
