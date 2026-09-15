param([string]$Version='v0.1.0')
$ErrorActionPreference = 'Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$cli=Join-Path $projectRoot '.tools\gh\bin\gh.exe'
$archive=Join-Path $projectRoot ('Builds\TicosHouse-'+$Version+'-Windows-x64.zip')
if(-not (Test-Path -LiteralPath $archive)){throw 'Verified build archive is missing'}
if(-not (Select-String -LiteralPath (Join-Path $projectRoot 'TestResults\integration.log') -Pattern 'TICOS_QA_PASS' -Quiet)){throw 'Player integration validation is missing'}
# Reuse the existing authenticated GitHub credential without printing or persisting it.
$credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
if($LASTEXITCODE -ne 0){throw 'GitHub credential unavailable'}
$credentialSecret=$credentialLines | Where-Object { $_.StartsWith('password=') } | Select-Object -First 1
if(-not $credentialSecret){throw 'GitHub credential unavailable'}
$env:GH_TOKEN=$credentialSecret.Substring(9)
try {
    & $cli release create $Version $archive --repo joaobuild/Ticos-House --title "Tico's House $Version" --notes-file (Join-Path $projectRoot 'Docs\RELEASE-NOTES.md') --target main
    if($LASTEXITCODE -ne 0){throw 'Release publication failed'}
    & $cli release view $Version --repo joaobuild/Ticos-House --json url,assets
} finally { Remove-Item Env:GH_TOKEN -ErrorAction SilentlyContinue; $credentialLines=$null; $credentialSecret=$null }
