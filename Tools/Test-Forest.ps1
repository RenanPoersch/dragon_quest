param(
    [string]$UnityEditorPath='C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor',
    [string]$WorkDirectory=(Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestForestChecks')
)
$ErrorActionPreference='Stop'
$forestRoot=Split-Path -Parent $PSScriptRoot
$forestData=Join-Path $UnityEditorPath 'Data'
$forestDotnet=Join-Path $forestData 'NetCoreRuntime/dotnet.exe'
$forestCsc=Get-ChildItem -LiteralPath (Join-Path $forestData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$forestDll=Join-Path $WorkDirectory 'DragonQuest.Scripts.dll'
$forestArgs=@('-nologo','-target:library','-nostdlib+',('-out:"'+$forestDll+'"'))
$forestArgs+=Get-ChildItem -LiteralPath (Join-Path $forestData 'NetStandard/ref/2.1.0') -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
$forestArgs+=Get-ChildItem -LiteralPath (Join-Path $forestData 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
$forestArgs+='-reference:"'+(Join-Path $forestRoot 'Library/ScriptAssemblies/Unity.InputSystem.dll')+'"'
$forestArgs+=Get-ChildItem -LiteralPath (Join-Path $forestRoot 'Assets/Scripts') -Filter '*.cs' -Recurse | ForEach-Object { '"'+$_.FullName+'"' }
$forestRsp=Join-Path $WorkDirectory 'scripts.rsp'
$forestArgs | Set-Content -LiteralPath $forestRsp
& $forestDotnet $forestCsc.FullName ('@'+$forestRsp)
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$forestPack=Get-ChildItem -LiteralPath (Join-Path $forestData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$forestRefs=Get-ChildItem -LiteralPath (Join-Path $forestPack.FullName 'ref') -Directory | Select-Object -First 1
$forestRuntime=Get-ChildItem -LiteralPath (Join-Path $forestData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$forestCore=Join-Path $forestData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
Copy-Item -LiteralPath $forestCore -Destination $WorkDirectory -Force
foreach($forestTest in @('ForestCoverChecks','ForestNavigationChecks'))
{
    $forestOut=Join-Path $WorkDirectory ($forestTest+'.dll')
    $forestArgs=@('-nologo','-target:exe','-nostdlib+',('-out:"'+$forestOut+'"'),('-reference:"'+$forestDll+'"'),('-reference:"'+$forestCore+'"'))
    $forestArgs+=Get-ChildItem -LiteralPath $forestRefs.FullName -Filter '*.dll' | ForEach-Object { '-reference:"'+$_.FullName+'"' }
    $forestArgs+='"'+(Join-Path $forestRoot ('Tests/'+$forestTest+'.cs'))+'"'
    $forestRsp=Join-Path $WorkDirectory 'checks.rsp'
    $forestArgs | Set-Content -LiteralPath $forestRsp
    & $forestDotnet $forestCsc.FullName ('@'+$forestRsp)
    if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
    @{runtimeOptions=@{tfm=$forestRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$forestRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory ($forestTest+'.runtimeconfig.json'))
    & $forestDotnet $forestOut
    if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
}
