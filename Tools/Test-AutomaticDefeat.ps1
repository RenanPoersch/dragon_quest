param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestAutomaticDefeatChecks'),
    [string]$InputPackagePath
)
$ErrorActionPreference = 'Stop'
$defeatRoot = Split-Path -Parent $PSScriptRoot
$defeatRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $defeatRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $defeatRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$defeatProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
foreach ($defeatFolder in @('Assets/Plugins', 'Assets/Editor', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $defeatProject $defeatFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $defeatRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $defeatProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $defeatRoot 'Tests/AutomaticDefeatUnityChecks.cs') -Destination (Join-Path $defeatProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $defeatRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $defeatProject 'ProjectSettings')
if (-not $InputPackagePath) {
    $defeatInput = Get-ChildItem -LiteralPath (Join-Path $defeatRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
    if (-not $defeatInput) { throw 'Importe o Input System na Unity antes dos testes.' }
    $InputPackagePath = $defeatInput.FullName
}
@{dependencies=@{'com.unity.inputsystem'=('file:' + $InputPackagePath.Replace('\', '/')); 'com.unity.modules.physics2d'='1.0.0'}} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $defeatProject 'Packages/manifest.json') -Encoding utf8
$defeatLog = Join-Path $defeatProject 'validation.log'
$defeatProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics',
    '-projectPath', ('"' + $defeatProject + '"'), '-executeMethod', 'AutomaticDefeatUnityChecks.Run', '-logFile', ('"' + $defeatLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Automatic defeat Unity PID $($defeatProcess.Id). Log: $defeatLog"
$defeatDeadline = [DateTime]::UtcNow.AddMinutes(3)
while (-not $defeatProcess.WaitForExit(30000)) {
    if ([DateTime]::UtcNow -ge $defeatDeadline) {
        $defeatProcess.Kill()
        throw "Tempo limite do teste Unity. Consulte $defeatLog"
    }
}
Get-Content -LiteralPath $defeatLog | Select-String 'AUTOMATIC_DEFEAT_UNITY_OK|Exception:|error CS'
if ($defeatProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $defeatLog -Pattern 'AUTOMATIC_DEFEAT_UNITY_OK' -Quiet)) {
    throw "Validacao da derrota automatica falhou. Consulte $defeatLog"
}
