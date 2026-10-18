param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestMusicChecks')
)
$ErrorActionPreference = 'Stop'
$musicRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $musicRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Compile')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# A fresh copy allows the user's Editor to remain open and preserves all real saves.
$musicProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
foreach ($musicFolder in @('Assets/Editor','ProjectSettings','Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $musicProject $musicFolder) -Force | Out-Null
}
foreach ($musicFolder in @('Scripts','Resources','Scenes')) {
    Copy-Item -LiteralPath (Join-Path $musicRoot "Assets/$musicFolder") -Destination (Join-Path $musicProject 'Assets') -Recurse
    Copy-Item -LiteralPath (Join-Path $musicRoot "Assets/$musicFolder.meta") -Destination (Join-Path $musicProject 'Assets')
}
foreach ($musicEditorFile in @('MusicImportSettings.cs','MusicImportSettings.cs.meta','MusicValidation.cs','MusicValidation.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $musicRoot "Assets/Editor/$musicEditorFile") -Destination (Join-Path $musicProject 'Assets/Editor')
}
foreach ($musicSettingsFile in @('ProjectVersion.txt','ProjectSettings.asset','AudioManager.asset')) {
    Copy-Item -LiteralPath (Join-Path $musicRoot "ProjectSettings/$musicSettingsFile") -Destination (Join-Path $musicProject 'ProjectSettings')
}
$musicInput = Get-ChildItem -LiteralPath (Join-Path $musicRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $musicInput) { throw 'Importe o Input System na Unity antes da validacao.' }
@{ dependencies = @{
    'com.unity.inputsystem' = ('file:' + $musicInput.FullName.Replace('\','/'))
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.physics2d' = '1.0.0'
    'com.unity.modules.imageconversion' = '1.0.0'
} } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $musicProject 'Packages/manifest.json') -Encoding utf8
$musicLog = Join-Path $musicProject 'validation.log'
$musicProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode','-force-d3d11',
    '-projectPath',('"' + $musicProject + '"'),'-executeMethod','MusicValidation.RunPlayback',
    '-logFile',('"' + $musicLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Music playback checks PID $($musicProcess.Id). Log: $musicLog"
$musicProcess.WaitForExit()
Get-Content -LiteralPath $musicLog | Select-String 'MUSIC .*PASS|Exception:|error CS'
if ($musicProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $musicLog -Pattern 'MUSIC PLAYBACK PASS' -Quiet)) {
    throw "Validacao de audio falhou. Consulte $musicLog"
}
