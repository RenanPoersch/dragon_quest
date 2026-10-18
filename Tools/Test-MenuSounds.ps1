param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestMenuSoundChecks')
)
$ErrorActionPreference = 'Stop'
$menuRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $menuRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Compile')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# A separate project preserves the user's open Editor, game scene and save files.
$menuProject = Join-Path $WorkDirectory ('Unity-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
foreach ($menuFolder in @('Assets/Editor','Assets/Resources','ProjectSettings','Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $menuProject $menuFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $menuRoot 'Assets/Scripts') -Destination (Join-Path $menuProject 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $menuRoot 'Assets/Scripts.meta') -Destination (Join-Path $menuProject 'Assets')
Copy-Item -LiteralPath (Join-Path $menuRoot 'Assets/Resources/MenuSounds') -Destination (Join-Path $menuProject 'Assets/Resources') -Recurse
Copy-Item -LiteralPath (Join-Path $menuRoot 'Assets/Resources/MenuSounds.meta') -Destination (Join-Path $menuProject 'Assets/Resources')
foreach ($menuFile in @('MenuSoundValidation.cs','MenuSoundValidation.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $menuRoot "Assets/Editor/$menuFile") -Destination (Join-Path $menuProject 'Assets/Editor')
}
foreach ($menuFile in @('ProjectVersion.txt','ProjectSettings.asset','AudioManager.asset')) {
    Copy-Item -LiteralPath (Join-Path $menuRoot "ProjectSettings/$menuFile") -Destination (Join-Path $menuProject 'ProjectSettings')
}
$menuInput = Get-ChildItem -LiteralPath (Join-Path $menuRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $menuInput) { throw 'Importe o Input System na Unity antes da validacao.' }
@{ dependencies = @{
    'com.unity.inputsystem' = ('file:' + $menuInput.FullName.Replace('\','/'))
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.physics2d' = '1.0.0'
    'com.unity.modules.imageconversion' = '1.0.0'
} } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $menuProject 'Packages/manifest.json') -Encoding utf8
$menuLog = Join-Path $menuProject 'validation.log'
$menuProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode','-force-d3d11',
    '-projectPath',('"' + $menuProject + '"'),'-executeMethod','MenuSoundValidation.RunPlayback',
    '-logFile',('"' + $menuLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Menu sound checks PID $($menuProcess.Id). Log: $menuLog"
$menuProcess.WaitForExit()
Get-Content -LiteralPath $menuLog | Select-String 'MENU SOUND.*PASS|Exception:|error CS'
if ($menuProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $menuLog -Pattern 'MENU SOUNDS PLAYBACK PASS' -Quiet)) {
    throw "Validacao de sons dos menus falhou. Consulte $menuLog"
}
