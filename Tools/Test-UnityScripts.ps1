param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor',
    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'DragonQuestScriptChecks')
)

# Compila scripts do jogo sem iniciar outra instancia do Editor ou alterar assets.
$ErrorActionPreference = 'Stop'
$scriptProjectRoot = Split-Path -Parent $PSScriptRoot
$scriptUnityData = Join-Path $UnityEditorPath 'Data'
$scriptDotnet = Join-Path $scriptUnityData 'NetCoreRuntime/dotnet.exe'
$scriptCsc = Get-ChildItem -LiteralPath (Join-Path $scriptUnityData 'DotNetSdk/sdk') -Filter csc.dll -Recurse | Select-Object -First 1
$scriptInputAssembly = Join-Path $scriptProjectRoot 'Library/ScriptAssemblies/Unity.InputSystem.dll'
if (-not $scriptCsc -or -not (Test-Path -LiteralPath $scriptInputAssembly)) { throw 'Abra e importe o projeto na Unity para gerar a biblioteca do Input System.' }
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$scriptArguments = @('-nologo', '-target:library', '-nostdlib+', "-out:$(Join-Path $WorkDirectory 'DragonQuest.Scripts.dll')")
$scriptArguments += Get-ChildItem -LiteralPath (Join-Path $scriptUnityData 'NetStandard/ref/2.1.0') -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$scriptArguments += Get-ChildItem -LiteralPath (Join-Path $scriptUnityData 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { "-reference:$($_.FullName)" }
$scriptArguments += "-reference:$scriptInputAssembly"
$scriptArguments += Get-ChildItem -LiteralPath (Join-Path $scriptProjectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName }
# O projeto cresceu: arquivo de resposta evita o limite de 32767 caracteres do Windows.
$scriptResponse = Join-Path $WorkDirectory 'UnityScripts.rsp'
$scriptArguments | ForEach-Object { '"' + $_ + '"' } | Set-Content -LiteralPath $scriptResponse -Encoding utf8
& $scriptDotnet $scriptCsc.FullName "@$scriptResponse"
if ($LASTEXITCODE -eq 0) { Write-Output 'Scripts do jogo compilaram com as bibliotecas da Unity.' }
exit $LASTEXITCODE
