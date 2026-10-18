param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestControllerChecks'),
    [switch]$UnityIntegration,
    [switch]$PlayMode
)
$ErrorActionPreference = 'Stop'
$controllerRoot = Split-Path -Parent $PSScriptRoot
$controllerData = Join-Path $UnityEditorPath 'Data'
$controllerDotnet = Join-Path $controllerData 'NetCoreRuntime/dotnet.exe'
$controllerSdk = Join-Path $controllerData 'DotNetSdk'
$controllerCsc = Get-ChildItem -LiteralPath (Join-Path $controllerSdk 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$controllerRefPack = Get-ChildItem -LiteralPath (Join-Path $controllerSdk 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$controllerRefs = Get-ChildItem -LiteralPath (Join-Path $controllerRefPack.FullName 'ref') -Directory | Select-Object -First 1
$controllerRuntime = Get-ChildItem -LiteralPath (Join-Path $controllerData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$controllerOutput = Join-Path $WorkDirectory 'ControllerChecks.dll'
$controllerArguments = @('-nologo', '-target:exe', '-nostdlib+', "-out:$controllerOutput")
$controllerArguments += Get-ChildItem -LiteralPath $controllerRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$controllerArguments += Join-Path $controllerRoot 'Assets/Scripts/Input/ControllerNavigation.cs'
$controllerArguments += Join-Path $controllerRoot 'Tests/ControllerChecks.cs'
& $controllerDotnet $controllerCsc.FullName @controllerArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$controllerConfig = @{runtimeOptions=@{tfm=$controllerRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$controllerRuntime.Name}}}
$controllerConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'ControllerChecks.runtimeconfig.json') -Encoding utf8
& $controllerDotnet $controllerOutput
if ($LASTEXITCODE -ne 0 -or -not $UnityIntegration) { exit $LASTEXITCODE }

# A separate, disposable project leaves the user's open Editor and real saves untouched.
$controllerProject = Join-Path $WorkDirectory 'Unity'
foreach ($controllerFolder in @('Assets/Editor', 'Assets/Plugins', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $controllerProject $controllerFolder) -Force | Out-Null
}
$controllerScriptOutput = Join-Path $WorkDirectory 'Runtime'
& (Join-Path $controllerRoot 'Tools/Test-UnityScripts.ps1') -UnityEditorPath $UnityEditorPath -WorkDirectory $controllerScriptOutput
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $controllerScriptOutput 'DragonQuest.Scripts.dll') -Destination (Join-Path $controllerProject 'Assets/Plugins')
Copy-Item -LiteralPath (Join-Path $controllerRoot 'Tests/ControllerUnityChecks.cs') -Destination (Join-Path $controllerProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $controllerRoot 'Tests/MenuInputUnityChecks.cs') -Destination (Join-Path $controllerProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $controllerRoot 'Tests/ControllerPlayModeChecks.cs') -Destination (Join-Path $controllerProject 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $controllerRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $controllerProject 'ProjectSettings')
$controllerPackage = Get-ChildItem -LiteralPath (Join-Path $controllerRoot 'Library/PackageCache') -Directory -Filter 'com.unity.inputsystem@*' | Select-Object -First 1
if (-not $controllerPackage) { throw 'Importe o Input System na Unity antes dos testes de integracao.' }
@{dependencies=@{'com.unity.inputsystem'=('file:' + $controllerPackage.FullName.Replace('\', '/'))}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $controllerProject 'Packages/manifest.json') -Encoding utf8
$controllerLog = Join-Path $controllerProject 'validation.log'
$controllerMethod = if ($PlayMode) { 'ControllerPlayModeChecks.Run' } else { 'ControllerUnityChecks.Run' }
$controllerProcess = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $controllerProject + '"'), '-executeMethod', $controllerMethod, '-logFile', ('"' + $controllerLog + '"')) -WindowStyle Hidden -PassThru
Write-Output "Unity integration PID $($controllerProcess.Id). Log: $controllerLog"
# Poll this process/log from the caller when running interactively; no second Editor touches the game project.
$controllerProcess.WaitForExit()
Get-Content -LiteralPath $controllerLog | Select-String 'CONTROLLER_UNITY_CHECKS_OK|MENU_INPUT_UNITY_CHECKS_OK|CONTROLLER_PLAYMODE_CHECKS_STARTED|^Exception:|error CS|Assertion failed'
if ($controllerProcess.ExitCode -ne 0) { throw "Unity integration failed. See $controllerLog" }
if (-not (Select-String -LiteralPath $controllerLog -Pattern 'CONTROLLER_UNITY_CHECKS_OK' -Quiet)) { throw "Unity did not confirm integration checks. See $controllerLog" }
