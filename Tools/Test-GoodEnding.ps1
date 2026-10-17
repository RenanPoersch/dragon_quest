param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestGoodEndingChecks')
)
$ErrorActionPreference = 'Stop'
$goodRoot = Split-Path -Parent $PSScriptRoot
$goodData = Join-Path $UnityEditorPath 'Data'
$goodDotnet = Join-Path $goodData 'NetCoreRuntime/dotnet.exe'
$goodCsc = Get-ChildItem -LiteralPath (Join-Path $goodData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$goodPack = Get-ChildItem -LiteralPath (Join-Path $goodData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$goodRefs = Get-ChildItem -LiteralPath (Join-Path $goodPack.FullName 'ref') -Directory | Select-Object -First 1
$goodRuntime = Get-ChildItem -LiteralPath (Join-Path $goodData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$goodOutput = Join-Path $WorkDirectory 'GoodEndingChecks.dll'
$goodArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$goodOutput")
$goodArguments += Get-ChildItem -LiteralPath $goodRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$goodArguments += Join-Path $goodRoot 'Assets/Scripts/Prototype/KingFreedSequence.cs'
$goodArguments += Join-Path $goodRoot 'Tests/KingFreedChecks.cs'
$goodResponse = Join-Path $WorkDirectory 'GoodEndingChecks.rsp'
$goodArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $goodResponse -Encoding utf8
& $goodDotnet $goodCsc.FullName "@$goodResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$goodRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$goodRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'GoodEndingChecks.runtimeconfig.json') -Encoding utf8
& $goodDotnet $goodOutput
exit $LASTEXITCODE
