param([string]$UnityPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $UnityPath) { $UnityPath = Join-Path $projectRoot '.tools\Unity\Editor\Unity.exe' }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw 'Unity editor not found' }
New-Item -ItemType Directory -Force (Join-Path $projectRoot 'TestResults') | Out-Null
$log = Join-Path $projectRoot 'TestResults\build.log'
& $UnityPath -batchmode -nographics -quit -projectPath $projectRoot -executeMethod BuildGame.Windows -logFile $log
if (-not (Select-String -LiteralPath $log -Pattern 'TICOS_BUILD_SUCCESS' -Quiet)) { throw "Build failed; see $log" }
