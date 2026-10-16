param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestSecretChecks')
)
$ErrorActionPreference = 'Stop'
$secretRoot = Split-Path -Parent $PSScriptRoot
$secretData = Join-Path $UnityEditorPath 'Data'
$secretDotnet = Join-Path $secretData 'NetCoreRuntime/dotnet.exe'
$secretSdk = Join-Path $secretData 'DotNetSdk'
$secretCsc = Get-ChildItem -LiteralPath (Join-Path $secretSdk 'sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$secretRefPack = Get-ChildItem -LiteralPath (Join-Path $secretSdk 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$secretRefs = Get-ChildItem -LiteralPath (Join-Path $secretRefPack.FullName 'ref') -Directory | Select-Object -First 1
$secretRuntime = Get-ChildItem -LiteralPath (Join-Path $secretData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$secretOut = Join-Path $WorkDirectory 'AncientSecretChecks.dll'
$secretArguments = @('-nologo','-target:exe','-nostdlib+',"-out:$secretOut")
$secretArguments += Get-ChildItem -LiteralPath $secretRefs.FullName -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$secretArguments += Join-Path $secretRoot 'Assets/Scripts/Progression/AncientSecrets.cs'
$secretArguments += Join-Path $secretRoot 'Assets/Scripts/Progression/LegendPoem.cs'
$secretArguments += Join-Path $secretRoot 'Assets/Scripts/Interactions/DialogueSession.cs'
$secretArguments += Join-Path $secretRoot 'Assets/Scripts/Interactions/DialogueTextReveal.cs'
$secretArguments += Join-Path $secretRoot 'Tests/AncientSecretChecks.cs'
& $secretDotnet $secretCsc.FullName @secretArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$secretConfig = @{runtimeOptions=@{tfm=$secretRefs.Name;framework=@{name='Microsoft.NETCore.App';version=$secretRuntime.Name}}}
$secretConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $WorkDirectory 'AncientSecretChecks.runtimeconfig.json') -Encoding utf8
& $secretDotnet $secretOut
exit $LASTEXITCODE
