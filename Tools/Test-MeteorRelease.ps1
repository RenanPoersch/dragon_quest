param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'DragonQuestMeteorReleaseChecks')
)
$ErrorActionPreference = 'Stop'
$meteorRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $meteorRoot 'Tools/Test-Castle.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Rules')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $meteorRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory (Join-Path $WorkDirectory 'Runtime')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# A separate project preserves the open game Editor and the user's saves.
$meteorProject = Join-Path $WorkDirectory 'Unity'
foreach ($meteorFolder in @('Assets/Editor', 'Assets/Plugins', 'Assets/Resources/PixelArt', 'Assets/Resources/BattleBackgrounds', 'Packages', 'ProjectSettings', 'Previews')) {
    New-Item -ItemType Directory -Path (Join-Path $meteorProject $meteorFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $WorkDirectory 'Runtime/DragonQuest.Scripts.dll') -Destination (Join-Path $meteorProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $meteorRoot 'Tests/MeteorReleaseUnityChecks.cs') -Destination (Join-Path $meteorProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $meteorRoot 'Assets/Editor/MeteorAbsorptionValidation.cs') -Destination (Join-Path $meteorProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $meteorRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $meteorProject 'ProjectSettings')
foreach ($meteorSprite in @('ArenMeteorDraconic', 'DraconicAbsorptionAura', 'TyrantFireballRelease', 'MeteorFireRibbons', 'TyrantProtect', 'TyrantShieldHit')) {
    foreach ($meteorSuffix in @('.png', '.png.meta')) {
        Copy-Item -LiteralPath (Join-Path $meteorRoot ('Assets/Resources/PixelArt/' + $meteorSprite + $meteorSuffix)) -Destination (Join-Path $meteorProject 'Assets/Resources/PixelArt')
    }
}
foreach ($meteorSuffix in @('.png', '.png.meta')) {
    Copy-Item -LiteralPath (Join-Path $meteorRoot ('Assets/Resources/BattleBackgrounds/castle' + $meteorSuffix)) -Destination (Join-Path $meteorProject 'Assets/Resources/BattleBackgrounds')
}
$meteorInputPackage = Get-ChildItem -LiteralPath (Join-Path $meteorRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $meteorInputPackage) { throw 'Importe o Input System na Unity antes da validacao.' }
@{dependencies=@{'com.unity.inputsystem'=('file:' + $meteorInputPackage.FullName.Replace('\', '/'))}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $meteorProject 'Packages/manifest.json') -Encoding utf8
$meteorLog = Join-Path $meteorProject 'validation.log'
$meteorPreviousOutput = $env:DQ_COMET_OUTPUT
try {
    $env:DQ_COMET_OUTPUT = Join-Path $meteorProject 'Previews'
    $meteorProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-projectPath', ('"' + $meteorProject + '"'), '-executeMethod', 'MeteorReleaseUnityChecks.Run', '-logFile', ('"' + $meteorLog + '"')) -WindowStyle Hidden -PassThru
    Write-Output "Unity validation PID $($meteorProcess.Id). Log: $meteorLog"
    $meteorProcess.WaitForExit()
} finally { $env:DQ_COMET_OUTPUT = $meteorPreviousOutput }
Get-Content -LiteralPath $meteorLog | Select-String 'METEOR_RELEASE_UNITY_OK|COMET_ABSORPTION_UNITY_OK|Exception:|error CS|Assertion failed'
if ($meteorProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $meteorLog -Pattern 'METEOR_RELEASE_UNITY_OK' -Quiet)) { throw "Validacao do meteoro falhou. Consulte $meteorLog" }
Write-Output "Previews: $(Join-Path $meteorProject 'Previews')"
