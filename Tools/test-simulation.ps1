$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Force (Join-Path $projectRoot 'TestResults') | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $projectRoot 'TestResults\SimulationTests.exe'
& $compiler /nologo /target:exe "/out:$testExe" (Join-Path $projectRoot 'Assets\Scripts\Gameplay\NightSimulation.cs') (Join-Path $projectRoot 'Tests\SimulationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Simulation compilation failed' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Simulation tests failed' }
