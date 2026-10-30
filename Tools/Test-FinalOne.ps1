param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestFinalOneChecks')
)
$ErrorActionPreference = 'Stop'
$finalRoot = Split-Path -Parent $PSScriptRoot
$finalData = Join-Path $UnityEditorPath 'Data'
$finalDotnet = Join-Path $finalData 'NetCoreRuntime/dotnet.exe'
$finalCsc = Get-ChildItem -LiteralPath (Join-Path $finalData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$finalPack = Get-ChildItem -LiteralPath (Join-Path $finalData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$finalRefs = Get-ChildItem -LiteralPath (Join-Path $finalPack.FullName 'ref') -Directory | Select-Object -First 1
$finalRuntime = Get-ChildItem -LiteralPath (Join-Path $finalData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$finalOutput = Join-Path $WorkDirectory 'FinalOneChecks.dll'
$finalArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$finalOutput")
$finalArguments += Get-ChildItem -LiteralPath $finalRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$finalArguments += Join-Path $finalRoot 'Assets/Scripts/Presentation/Cinematics/Endings/CastleDestroyedSequence.cs'
$finalArguments += Join-Path $finalRoot 'Tests/FinalOneChecks.cs'
$finalResponse = Join-Path $WorkDirectory 'FinalOneChecks.rsp'
$finalArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $finalResponse -Encoding utf8
& $finalDotnet $finalCsc.FullName "@$finalResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$finalRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$finalRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'FinalOneChecks.runtimeconfig.json') -Encoding utf8
& $finalDotnet $finalOutput
exit $LASTEXITCODE
