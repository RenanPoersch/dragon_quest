param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestBattleChecks')
)

$ErrorActionPreference = 'Stop'
$testProjectRoot = Split-Path -Parent $PSScriptRoot
$testUnityData = Join-Path $UnityEditorPath 'Data'
$testDotnet = Join-Path $testUnityData 'NetCoreRuntime/dotnet.exe'
$testSdkRoot = Join-Path $testUnityData 'DotNetSdk'
$testCsc = Get-ChildItem -LiteralPath (Join-Path $testSdkRoot 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$testRefFolder = Get-ChildItem -LiteralPath (Join-Path $testSdkRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$testRuntimeFolder = Get-ChildItem -LiteralPath (Join-Path $testUnityData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
if (-not $testCsc -or -not $testRefFolder -or -not $testRuntimeFolder) { throw 'SDK .NET da Unity nao encontrado.' }
$testReferenceDirectory = Get-ChildItem -LiteralPath (Join-Path $testRefFolder.FullName 'ref') -Directory | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$testOutputFile = Join-Path $WorkDirectory 'BattleChecks.dll'
$testArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$testOutputFile")
$testArguments += Get-ChildItem -LiteralPath $testReferenceDirectory.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$testArguments += Get-ChildItem -LiteralPath (Join-Path $testProjectRoot 'Assets/Scripts/Combat') -Filter '*.cs' | ForEach-Object { $_.FullName }
$testArguments += Get-ChildItem -LiteralPath (Join-Path $testProjectRoot 'Assets/Scripts/Equipment') -Filter '*.cs' | ForEach-Object { $_.FullName }
$testArguments += Get-ChildItem -LiteralPath (Join-Path $testProjectRoot 'Assets/Scripts/Inventory') -Filter '*.cs' | ForEach-Object { $_.FullName }
$testArguments += Join-Path $testProjectRoot 'Assets/Scripts/Prototype/PrototypeBattleFactory.cs'
$testArguments += Join-Path $testProjectRoot 'Assets/Scripts/Prototype/PrototypePartyFactory.cs'
$testArguments += Join-Path $testProjectRoot 'Tests/BattleChecks.cs'
$testArguments += Join-Path $testProjectRoot 'Tests/AbilityChecks.cs'
$testArguments += Join-Path $testProjectRoot 'Tests/MateriaLimitChecks.cs'
$testArguments += Join-Path $testProjectRoot 'Tests/EquipmentItemChecks.cs'
& $testDotnet $testCsc.FullName @testArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$testRuntimeConfig = @{ runtimeOptions = @{ tfm = $testReferenceDirectory.Name; framework = @{ name = 'Microsoft.NETCore.App'; version = $testRuntimeFolder.Name } } }
$testRuntimeConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'BattleChecks.runtimeconfig.json') -Encoding utf8
& $testDotnet $testOutputFile
exit $LASTEXITCODE
