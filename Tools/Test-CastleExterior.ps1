param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestCastleExteriorChecks')
)

# Compila o jogo e verifica sobreposicoes no castelo sem abrir outro Editor.
$ErrorActionPreference = 'Stop'
$exteriorRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$exteriorData = Join-Path $UnityEditorPath 'Data'
$exteriorDotnet = Join-Path $exteriorData 'NetCoreRuntime/dotnet.exe'
$exteriorSdk = Join-Path $exteriorData 'DotNetSdk'
$exteriorCsc = Get-ChildItem -LiteralPath (Join-Path $exteriorSdk 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$exteriorRefPack = Get-ChildItem -LiteralPath (Join-Path $exteriorSdk 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$exteriorRefs = Get-ChildItem -LiteralPath (Join-Path $exteriorRefPack.FullName 'ref') -Directory | Select-Object -First 1
$exteriorRuntime = Get-ChildItem -LiteralPath (Join-Path $exteriorData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$exteriorCore = Join-Path $exteriorData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$exteriorOut = Join-Path $WorkDirectory 'CastleExteriorChecks.dll'
$exteriorArguments = @('-nologo','-target:exe','-nostdlib+',"-out:$exteriorOut","-reference:$exteriorCore","-reference:$(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')")
$exteriorArguments += Get-ChildItem -LiteralPath $exteriorRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$exteriorArguments += Join-Path $exteriorRoot 'Tests/CastleExteriorChecks.cs'
& $exteriorDotnet $exteriorCsc.FullName @exteriorArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $exteriorCore -Destination $WorkDirectory -Force
$exteriorConfig = @{runtimeOptions=@{tfm=$exteriorRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$exteriorRuntime.Name}}}
$exteriorConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'CastleExteriorChecks.runtimeconfig.json') -Encoding utf8
& $exteriorDotnet $exteriorOut $WorkDirectory
exit $LASTEXITCODE
