param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestNewGameVoyageChecks')
)
$ErrorActionPreference = 'Stop'
$introRoot = Split-Path -Parent $PSScriptRoot
$introData = Join-Path $UnityEditorPath 'Data'
$introDotnet = Join-Path $introData 'NetCoreRuntime/dotnet.exe'
$introSdk = Join-Path $introData 'DotNetSdk'
$introCsc = Get-ChildItem -LiteralPath (Join-Path $introSdk 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$introRefPack = Get-ChildItem -LiteralPath (Join-Path $introSdk 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$introRefs = Get-ChildItem -LiteralPath (Join-Path $introRefPack.FullName 'ref') -Directory | Select-Object -First 1
$introRuntime = Get-ChildItem -LiteralPath (Join-Path $introData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$introOutput = Join-Path $WorkDirectory 'NewGameVoyageChecks.dll'
$introArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$introOutput")
$introArguments += Get-ChildItem -LiteralPath $introRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$introArguments += Join-Path $introRoot 'Assets/Scripts/Prototype/NewGameVoyageSequence.cs'
$introArguments += Join-Path $introRoot 'Assets/Scripts/Prototype/CastleDestroyedSequence.cs'
$introArguments += Join-Path $introRoot 'Tests/NewGameVoyageChecks.cs'
& $introDotnet $introCsc.FullName @introArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$introConfig = @{runtimeOptions=@{tfm=$introRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$introRuntime.Name}}}
$introConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'NewGameVoyageChecks.runtimeconfig.json') -Encoding utf8
& $introDotnet $introOutput
exit $LASTEXITCODE
