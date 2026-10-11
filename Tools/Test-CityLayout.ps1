param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestCityChecks')
)

# Verifica caminhos na geometria do mapa sem iniciar outra instancia da Unity.
$ErrorActionPreference = 'Stop'
$cityProjectRoot = Split-Path -Parent $PSScriptRoot
$cityUnityData = Join-Path $UnityEditorPath 'Data'
$cityDotnet = Join-Path $cityUnityData 'NetCoreRuntime/dotnet.exe'
$citySdkRoot = Join-Path $cityUnityData 'DotNetSdk'
$cityCsc = Get-ChildItem -LiteralPath (Join-Path $citySdkRoot 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$cityRefFolder = Get-ChildItem -LiteralPath (Join-Path $citySdkRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$cityRuntimeFolder = Get-ChildItem -LiteralPath (Join-Path $cityUnityData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
if (-not $cityCsc -or -not $cityRefFolder -or -not $cityRuntimeFolder) { throw 'SDK .NET da Unity nao encontrado.' }
$cityReferenceDirectory = Get-ChildItem -LiteralPath (Join-Path $cityRefFolder.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$cityOutputFile = Join-Path $WorkDirectory 'CityLayoutChecks.dll'
$cityCoreAssembly = Join-Path $cityUnityData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'
$cityArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$cityOutputFile", "-reference:$cityCoreAssembly")
$cityArguments += Get-ChildItem -LiteralPath $cityReferenceDirectory.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$cityArguments += Join-Path $cityProjectRoot 'Assets/Scripts/Prototype/PrototypeCityLayout.cs'
$cityArguments += Join-Path $cityProjectRoot 'Tests/CityLayoutChecks.cs'
& $cityDotnet $cityCsc.FullName @cityArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $cityCoreAssembly -Destination $WorkDirectory -Force
$cityRuntimeConfig = @{ runtimeOptions = @{ tfm = $cityReferenceDirectory.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $cityRuntimeFolder.Name } } }
$cityRuntimeConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'CityLayoutChecks.runtimeconfig.json') -Encoding utf8
& $cityDotnet $cityOutputFile (Join-Path $WorkDirectory 'CityGeometry.json')
exit $LASTEXITCODE
