param(
    [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestLairChecks')
)
$ErrorActionPreference = 'Stop'
$lairRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $lairRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$lairData = Join-Path $UnityEditorPath 'Data'
$lairDotnet = Join-Path $lairData 'NetCoreRuntime/dotnet.exe'
$lairCsc = Get-ChildItem (Join-Path $lairData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$lairPack = Get-ChildItem (Join-Path $lairData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$lairRefs = Get-ChildItem (Join-Path $lairPack.FullName 'ref') -Directory | Select-Object -First 1
$lairRuntime = Get-ChildItem (Join-Path $lairData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$lairCore = Join-Path $lairData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$lairOutput = Join-Path $WorkDirectory 'DragonLairChecks.dll'
$lairArgs = @('-nologo','-target:exe','-nostdlib+',('-out:"'+$lairOutput+'"'),('-reference:"'+$lairCore+'"'),('-reference:"'+(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')+'"'))
$lairArgs += Get-ChildItem $lairRefs.FullName -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
$lairArgs += '"'+(Join-Path $lairRoot 'Tests/DragonLairChecks.cs')+'"'
$lairArgs += '"'+(Join-Path $lairRoot 'Tests/DragonChecks.cs')+'"'
$lairRsp = Join-Path $WorkDirectory 'lair.rsp'
$lairArgs | Set-Content -LiteralPath $lairRsp -Encoding utf8
& $lairDotnet $lairCsc.FullName ('@'+$lairRsp)
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $lairCore -Destination $WorkDirectory -Force
@{runtimeOptions=@{tfm=$lairRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$lairRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $WorkDirectory 'DragonLairChecks.runtimeconfig.json') -Encoding utf8
& $lairDotnet $lairOutput
exit $LASTEXITCODE
