param([string]$GameRoot = (Split-Path -Parent $PSScriptRoot), [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestUnifiedChecks'))
$ErrorActionPreference='Stop'
& (Join-Path $GameRoot 'Tools\Test-UnityScripts.ps1') -WorkDirectory $WorkDirectory
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
$unityData='C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Data'
$dotnetPath=Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$compiler=Get-ChildItem (Join-Path $unityData 'DotNetSdk\sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$pack=Get-ChildItem (Join-Path $unityData 'DotNetSdk\packs\Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$refs=Get-ChildItem (Join-Path $pack.FullName 'ref') -Directory | Select-Object -First 1
$arguments=@('-nologo','-target:exe','-nostdlib+',('-reference:'+(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')),('-reference:'+(Join-Path $unityData 'Managed\UnityEngine\UnityEngine.CoreModule.dll')))
$arguments+=Get-ChildItem $refs.FullName -Filter '*.dll' | ForEach-Object {'-reference:'+$_.FullName}
$output=Join-Path $WorkDirectory 'UnifiedWorldChecks.dll'
& $dotnetPath $compiler.FullName @arguments ('-out:'+$output) (Join-Path $GameRoot 'Tests\UnifiedWorldChecks.cs')
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
$runtime=Get-ChildItem (Join-Path $unityData 'NetCoreRuntime\shared\Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
@{runtimeOptions=@{tfm=$refs.Name;framework=@{name='Microsoft.NETCore.App';version=$runtime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $WorkDirectory 'UnifiedWorldChecks.runtimeconfig.json') -Encoding utf8
Copy-Item (Join-Path $unityData 'Managed\UnityEngine\UnityEngine.CoreModule.dll') $WorkDirectory -Force
& $dotnetPath $output
exit $LASTEXITCODE
