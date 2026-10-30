param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestObliqueChecks')
)

# Compila o jogo e verifica modelos/projecao sem abrir outra instancia do Editor.
$ErrorActionPreference = 'Stop'
$obliqueRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$obliqueData = Join-Path $UnityEditorPath 'Data'
$obliqueDotnet = Join-Path $obliqueData 'NetCoreRuntime/dotnet.exe'
$obliqueSdk = Join-Path $obliqueData 'DotNetSdk'
$obliqueCsc = Get-ChildItem -LiteralPath (Join-Path $obliqueSdk 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$obliqueRefPack = Get-ChildItem -LiteralPath (Join-Path $obliqueSdk 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$obliqueRefs = Get-ChildItem -LiteralPath (Join-Path $obliqueRefPack.FullName 'ref') -Directory | Select-Object -First 1
$obliqueRuntime = Get-ChildItem -LiteralPath (Join-Path $obliqueData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$obliqueCore = Join-Path $obliqueData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$obliqueOut = Join-Path $WorkDirectory 'ObliqueChecks.dll'
$obliqueArguments = @('-nologo','-target:exe','-nostdlib+',"-out:$obliqueOut","-reference:$obliqueCore","-reference:$(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')")
$obliqueArguments += Get-ChildItem -LiteralPath $obliqueRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$obliqueArguments += Join-Path $obliqueRoot 'Assets/Editor/Tools/LegacySceneryGeometry.cs'
$obliqueArguments += Join-Path $obliqueRoot 'Tests/ObliqueChecks.cs'
$obliqueArguments += Join-Path $obliqueRoot 'Tests/PixelArtChecks.cs'
& $obliqueDotnet $obliqueCsc.FullName @obliqueArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $obliqueCore -Destination $WorkDirectory -Force
$obliqueConfig = @{runtimeOptions=@{tfm=$obliqueRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$obliqueRuntime.Name}}}
$obliqueConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'ObliqueChecks.runtimeconfig.json') -Encoding utf8
& $obliqueDotnet $obliqueOut $WorkDirectory
exit $LASTEXITCODE
