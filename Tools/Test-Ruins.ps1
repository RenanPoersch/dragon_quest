param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot), [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestRuinsChecks'))
$ErrorActionPreference='Stop'
$ruinsData='C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Data'
$ruinsDotnet=Join-Path $ruinsData 'NetCoreRuntime/dotnet.exe'
$ruinsCsc=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
 $ruinsOutput=$WorkDirectory
New-Item -ItemType Directory -Path $ruinsOutput -Force | Out-Null
function Invoke-RuinsCompiler([string[]]$CompilerArguments) {
    $ruinsResponse=Join-Path $ruinsOutput 'compile.rsp'
    $CompilerArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $ruinsResponse -Encoding utf8
    & $ruinsDotnet $ruinsCsc.FullName "@$ruinsResponse"
    if($LASTEXITCODE -ne 0) {throw 'Falha na compilacao das ruinas.'}
}
$ruinsStaged=@()
$ruinsOriginal=Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse
$ruinsReferences=@()
$ruinsReferences+=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'NetStandard/ref/2.1.0') -Filter '*.dll' | ForEach-Object {"-reference:$($_.FullName)"}
$ruinsReferences+=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object {"-reference:$($_.FullName)"}
$ruinsReferences+="-reference:$(Join-Path $ProjectRoot 'Library/ScriptAssemblies/Unity.InputSystem.dll')"
$ruinsAssembly=Join-Path $ruinsOutput 'DragonQuest.Scripts.dll'
Invoke-RuinsCompiler (@('-nologo','-target:library','-nostdlib+',"-out:$ruinsAssembly") + $ruinsReferences + $ruinsOriginal.FullName)
if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
Write-Output 'Scripts atualizados compilaram com as bibliotecas da Unity.'
$ruinsEditorRefs=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'Managed') -Filter 'UnityEditor*.dll' -Recurse | ForEach-Object {"-reference:$($_.FullName)"}
Invoke-RuinsCompiler (@('-nologo','-target:library','-nostdlib+',"-out:$(Join-Path $ruinsOutput 'RuinsEditorChecks.dll')") + $ruinsReferences + $ruinsEditorRefs + @("-reference:$ruinsAssembly",(Join-Path $ProjectRoot 'Assets/Editor/RuinsSideValidation.cs')))
if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
$ruinsRefPack=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$ruinsRefDir=Get-ChildItem -LiteralPath (Join-Path $ruinsRefPack.FullName 'ref') -Directory | Select-Object -First 1
$ruinsRuntime=Get-ChildItem -LiteralPath (Join-Path $ruinsData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$ruinsHarness=Join-Path $ruinsOutput 'RuinsPathChecks.dll'
$ruinsCore=Join-Path $ruinsData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$ruinsNetRefs=Get-ChildItem -LiteralPath $ruinsRefDir.FullName -Filter '*.dll' | ForEach-Object {"-reference:$($_.FullName)"}
Invoke-RuinsCompiler (@('-nologo','-target:exe','-nostdlib+',"-out:$ruinsHarness") + $ruinsNetRefs + @("-reference:$ruinsCore","-reference:$ruinsAssembly",(Join-Path $ProjectRoot 'Tests/RuinsPathChecks.cs')))
if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
Copy-Item -LiteralPath $ruinsCore -Destination $ruinsOutput -Force
@{runtimeOptions=@{tfm=$ruinsRefDir.Name;framework=@{name='Microsoft.NETCore.App';version=$ruinsRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ruinsOutput 'RuinsPathChecks.runtimeconfig.json') -Encoding utf8
& $ruinsDotnet $ruinsHarness
exit $LASTEXITCODE
