param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestInnRestChecks')
)
$ErrorActionPreference = 'Stop'
$innProjectRoot = Split-Path -Parent $PSScriptRoot
$innRuntime = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $PSScriptRoot 'Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $innRuntime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Projeto descartavel: valida imagens e comportamento sem abrir o projeto do usuario.
$innProject = Join-Path $WorkDirectory 'Unity'
foreach ($innFolder in @('Assets/Editor','Assets/Plugins','Assets/Resources/PixelArt','Packages','ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $innProject $innFolder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $innRuntime 'DragonQuest.Scripts.dll') -Destination (Join-Path $innProject 'Assets/Plugins') -Force
foreach ($innValidator in @('InnValidation.cs','InnRestValidation.cs')) {
    Copy-Item -LiteralPath (Join-Path $innProjectRoot "Assets/Editor/$innValidator") -Destination (Join-Path $innProject 'Assets/Editor') -Force
}
Copy-Item -LiteralPath (Join-Path $innProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $innProject 'ProjectSettings') -Force
Get-ChildItem -LiteralPath (Join-Path $innProjectRoot 'Assets/Resources') -File -Filter '*.shader*' |
    Copy-Item -Destination (Join-Path $innProject 'Assets/Resources') -Force
foreach ($innImage in @('InnInterior','InnSavePoint','ArenWalk','ArenDiagonalWalk','LiaWalk','BramWalk','NpcRoster',
    'TerrainAtlas','CityMaterialsAtlas','CityDetailsAtlas','CastleMaterialsAtlas','CityGroundMap','WorldGroundMap')) {
    foreach ($innExtension in @('.png','.png.meta')) {
        Copy-Item -LiteralPath (Join-Path $innProjectRoot "Assets/Resources/PixelArt/$innImage$innExtension") -Destination (Join-Path $innProject 'Assets/Resources/PixelArt') -Force
    }
}
# Os shaders do jogo exigem o mesmo URP 2D, inclusive na validacao.
Copy-Item -LiteralPath (Join-Path $innProjectRoot 'Assets/Settings') -Destination (Join-Path $innProject 'Assets') -Recurse -Force
foreach ($innSetting in @('GraphicsSettings.asset','QualitySettings.asset')) {
    Copy-Item -LiteralPath (Join-Path $innProjectRoot "ProjectSettings/$innSetting") -Destination (Join-Path $innProject 'ProjectSettings') -Force
}
$innDependencies=@{}
$innQueue=[Collections.Generic.Queue[string]]::new()
$innQueue.Enqueue('com.unity.inputsystem')
$innQueue.Enqueue('com.unity.render-pipelines.universal')
while ($innQueue.Count -gt 0) {
    $innName=$innQueue.Dequeue()
    if ($innDependencies.ContainsKey($innName) -or $innName.StartsWith('com.unity.modules.')) { continue }
    $innCached=Get-ChildItem -LiteralPath (Join-Path $innProjectRoot 'Library/PackageCache') -Directory -Filter "$innName@*" | Select-Object -First 1
    if (-not $innCached) { throw "Importe $innName na Unity antes dos testes." }
    $innDependencies[$innName]='file:'+$innCached.FullName.Replace('\','/')
    $innPackageJson=Get-Content -LiteralPath (Join-Path $innCached.FullName 'package.json') -Raw | ConvertFrom-Json
    if ($innPackageJson.dependencies) {
        foreach ($innDependency in $innPackageJson.dependencies.PSObject.Properties.Name) { $innQueue.Enqueue($innDependency) }
    }
}
@{dependencies=$innDependencies} | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $innProject 'Packages/manifest.json') -Encoding utf8
$innLog = Join-Path $innProject 'validation.log'
$innPreviousOutput = $env:DQ_INN_OUTPUT
try {
    $env:DQ_INN_OUTPUT = Join-Path $WorkDirectory 'Preview'
    $innProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode','-force-d3d11','-quit','-projectPath',('"' + $innProject + '"'),'-executeMethod','InnRestValidation.Run','-logFile',('"' + $innLog + '"')) -WindowStyle Hidden -PassThru
    Write-Output "Unity Inn validation PID $($innProcess.Id). Log: $innLog"
    $innProcess.WaitForExit()
    Get-Content -LiteralPath $innLog | Select-String 'INN_VALIDATION_OK|INN_REST_VALIDATION_OK|^Exception:|error CS|Assertion failed'
    if ($innProcess.ExitCode -ne 0) { throw "Unity Inn validation failed. See $innLog" }
    if (-not (Select-String -LiteralPath $innLog -Pattern 'INN_REST_VALIDATION_OK' -Quiet)) { throw "Unity did not confirm Inn checks. See $innLog" }
}
finally { $env:DQ_INN_OUTPUT = $innPreviousOutput }
