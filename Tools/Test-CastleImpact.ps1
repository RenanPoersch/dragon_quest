param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestCastleImpactChecks')
)
$ErrorActionPreference = 'Stop'
$impactRoot = Split-Path -Parent $PSScriptRoot
$impactData = Join-Path $UnityEditorPath 'Data'
$impactDotnet = Join-Path $impactData 'NetCoreRuntime/dotnet.exe'
$impactCsc = Get-ChildItem -LiteralPath (Join-Path $impactData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$impactPack = Get-ChildItem -LiteralPath (Join-Path $impactData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$impactRefs = Get-ChildItem -LiteralPath (Join-Path $impactPack.FullName 'ref') -Directory | Select-Object -First 1
$impactRuntime = Get-ChildItem -LiteralPath (Join-Path $impactData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$impactOutput = Join-Path $WorkDirectory 'CastleImpactChecks.dll'
$impactArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$impactOutput")
$impactArguments += Get-ChildItem -LiteralPath $impactRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$impactArguments += Join-Path $impactRoot 'Assets/Scripts/Prototype/CastleImpactSequence.cs'
$impactArguments += Join-Path $impactRoot 'Tests/CastleImpactChecks.cs'
$impactResponse = Join-Path $WorkDirectory 'CastleImpactChecks.rsp'
$impactArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $impactResponse -Encoding utf8
& $impactDotnet $impactCsc.FullName "@$impactResponse"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{runtimeOptions=@{tfm=$impactRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$impactRuntime.Name}}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'CastleImpactChecks.runtimeconfig.json') -Encoding utf8
& $impactDotnet $impactOutput
exit $LASTEXITCODE
