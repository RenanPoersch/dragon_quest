param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestInventorySubmenuChecks')
)
$ErrorActionPreference = 'Stop'
$submenuRoot = Split-Path -Parent $PSScriptRoot
$submenuRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $submenuRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $submenuRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$submenuProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
foreach ($submenuFolder in @('Assets/Plugins', 'Assets/Editor', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $submenuProject $submenuFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $submenuRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $submenuProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $submenuRoot 'Tests/InventorySubmenuUnityChecks.cs') -Destination (Join-Path $submenuProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $submenuRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $submenuProject 'ProjectSettings')
$submenuInput = Get-ChildItem -LiteralPath (Join-Path $submenuRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $submenuInput) { throw 'Importe o Input System na Unity antes dos testes.' }
@{ dependencies = @{ 'com.unity.inputsystem' = ('file:' + $submenuInput.FullName.Replace('\', '/')); 'com.unity.modules.physics2d' = '1.0.0' } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $submenuProject 'Packages/manifest.json') -Encoding utf8
$submenuLog = Join-Path $submenuProject 'validation.log'
$submenuProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics',
    '-projectPath', ('"' + $submenuProject + '"'), '-executeMethod', 'InventorySubmenuUnityChecks.Run',
    '-logFile', ('"' + $submenuLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Inventory submenu Unity PID $($submenuProcess.Id). Log: $submenuLog"
if (-not $submenuProcess.WaitForExit(180000)) {
    Stop-Process -Id $submenuProcess.Id
    throw "Tempo limite na Unity. Consulte $submenuLog"
}
Get-Content -LiteralPath $submenuLog | Select-String 'INVENTORY_SUBMENU_UNITY_CHECKS_OK|Exception:|error CS'
if ($submenuProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $submenuLog -Pattern 'INVENTORY_SUBMENU_UNITY_CHECKS_OK' -Quiet)) {
    throw "Validacao dos submenus falhou. Consulte $submenuLog"
}
