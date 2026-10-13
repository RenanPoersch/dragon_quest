param(
    [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestQuarryChecks')
)
$ErrorActionPreference = 'Stop'
$quarryRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $quarryRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$quarryData = Join-Path $UnityEditorPath 'Data'
$quarryDotnet = Join-Path $quarryData 'NetCoreRuntime/dotnet.exe'
$quarryCsc = Get-ChildItem (Join-Path $quarryData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$quarryPack = Get-ChildItem (Join-Path $quarryData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$quarryRefs = Get-ChildItem (Join-Path $quarryPack.FullName 'ref') -Directory | Select-Object -First 1
$quarryRuntime = Get-ChildItem (Join-Path $quarryData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$quarryCore = Join-Path $quarryData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$quarryOutput = Join-Path $WorkDirectory 'QuarryChecks.dll'
$quarryArgs = @('-nologo','-target:exe','-nostdlib+',('-out:"'+$quarryOutput+'"'),('-reference:"'+$quarryCore+'"'),('-reference:"'+(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')+'"'))
$quarryArgs += Get-ChildItem $quarryRefs.FullName -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
$quarryArgs += '"'+(Join-Path $quarryRoot 'Tests/QuarryChecks.cs')+'"'
$quarryRsp = Join-Path $WorkDirectory 'quarry.rsp'
$quarryArgs | Set-Content -LiteralPath $quarryRsp -Encoding utf8
& $quarryDotnet $quarryCsc.FullName ('@'+$quarryRsp)
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $quarryCore -Destination $WorkDirectory -Force
@{runtimeOptions=@{tfm=$quarryRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$quarryRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $WorkDirectory 'QuarryChecks.runtimeconfig.json') -Encoding utf8
& $quarryDotnet $quarryOutput
exit $LASTEXITCODE
