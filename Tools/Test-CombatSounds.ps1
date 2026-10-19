param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestCombatSounds')
)
$ErrorActionPreference = 'Stop'
$combatRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $combatRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Compile')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Validate in a new empty scene/project, preserving the open Editor and player saves.
$combatProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
foreach ($folder in @('Assets/Editor','Assets/Resources','ProjectSettings','Packages','Docs')) {
    New-Item -ItemType Directory -Path (Join-Path $combatProject $folder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $combatRoot 'Assets/Scripts') -Destination (Join-Path $combatProject 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $combatRoot 'Assets/Scripts.meta') -Destination (Join-Path $combatProject 'Assets')
Copy-Item -LiteralPath (Join-Path $combatRoot 'Assets/Resources/CombatSounds') -Destination (Join-Path $combatProject 'Assets/Resources') -Recurse
Copy-Item -LiteralPath (Join-Path $combatRoot 'Assets/Resources/CombatSounds.meta') -Destination (Join-Path $combatProject 'Assets/Resources')
foreach ($file in @('CombatSoundValidation.cs','CombatSoundValidation.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $combatRoot "Assets/Editor/$file") -Destination (Join-Path $combatProject 'Assets/Editor')
}
Copy-Item -LiteralPath (Join-Path $combatRoot 'Docs/CombatSoundManifest.json') -Destination (Join-Path $combatProject 'Docs')
foreach ($file in @('ProjectVersion.txt','ProjectSettings.asset','AudioManager.asset')) {
    Copy-Item -LiteralPath (Join-Path $combatRoot "ProjectSettings/$file") -Destination (Join-Path $combatProject 'ProjectSettings')
}
$combatInput = Get-ChildItem -LiteralPath (Join-Path $combatRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $combatInput) { throw 'Importe o Input System antes da validacao.' }
@{ dependencies = @{
    'com.unity.inputsystem' = ('file:' + $combatInput.FullName.Replace('\','/'))
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.physics2d' = '1.0.0'
    'com.unity.modules.imageconversion' = '1.0.0'
} } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $combatProject 'Packages/manifest.json') -Encoding utf8
$combatLog = Join-Path $combatProject 'validation.log'
$combatProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode','-force-d3d11',
    '-projectPath',('"' + $combatProject + '"'),'-executeMethod','CombatSoundValidation.RunPlayback',
    '-logFile',('"' + $combatLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Combat sound checks PID $($combatProcess.Id). Log: $combatLog"
$combatProcess.WaitForExit()
Get-Content -LiteralPath $combatLog | Select-String 'COMBAT SOUND.*PASS|Exception:|error CS'
if ($combatProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $combatLog -Pattern 'COMBAT SOUNDS PLAYBACK PASS' -Quiet)) {
    throw "Validacao de sons de combate falhou. Consulte $combatLog"
}
