$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet build Clinic.sln
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path (Get-Location) '.local/browsers'
powershell -ExecutionPolicy Bypass -File tests/Clinic.Smoke/bin/Debug/net10.0/playwright.ps1 install chromium
if ($LASTEXITCODE -ne 0) { throw 'Failed to install Chromium for browser tests.' }
dotnet run --project tests/Clinic.Smoke --no-build
if ($LASTEXITCODE -ne 0) { throw 'Tests failed. Check TestResults/smoke-results.json.' }
