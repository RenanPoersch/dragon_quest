param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestFieldMenuUnityChecks'),
    [switch]$ExitToTitle
)
$ErrorActionPreference = 'Stop'
$fieldRoot = Split-Path -Parent $PSScriptRoot
$fieldRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $fieldRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $fieldRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$fieldProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
foreach ($fieldFolder in @('Assets/Plugins', 'Assets/Editor', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $fieldProject $fieldFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $fieldRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $fieldProject 'Assets/Plugins')
$fieldCheck = if ($ExitToTitle) { 'ExitMenuUnityChecks' } else { 'FieldMenuUnityChecks' }
$fieldMarker = if ($ExitToTitle) { 'EXIT_MENU_UNITY_CHECKS_OK' } else { 'FIELD_MENU_UNITY_CHECKS_OK' }
Copy-Item -LiteralPath (Join-Path $fieldRoot ('Tests/' + $fieldCheck + '.cs')) -Destination (Join-Path $fieldProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $fieldRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $fieldProject 'ProjectSettings')
$fieldInput = Get-ChildItem -LiteralPath (Join-Path $fieldRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $fieldInput) { throw 'Importe o Input System na Unity antes dos testes.' }
@{ dependencies = @{ 'com.unity.inputsystem' = ('file:' + $fieldInput.FullName.Replace('\', '/')); 'com.unity.modules.physics2d' = '1.0.0' } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fieldProject 'Packages/manifest.json') -Encoding utf8
$fieldLog = Join-Path $fieldProject 'validation.log'
$fieldGraphics = if ($ExitToTitle) { '-force-d3d11' } else { '-nographics' }
$fieldProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', $fieldGraphics,
    '-projectPath', ('"' + $fieldProject + '"'), '-executeMethod', ($fieldCheck + '.Run'), '-logFile', ('"' + $fieldLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Field menu Unity PID $($fieldProcess.Id). Log: $fieldLog"
$fieldProcess.WaitForExit()
Get-Content -LiteralPath $fieldLog | Select-String ($fieldMarker + '|Exception:|error CS')
if ($fieldProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $fieldLog -Pattern $fieldMarker -Quiet)) {
    throw "Validacao do menu falhou. Consulte $fieldLog"
}
