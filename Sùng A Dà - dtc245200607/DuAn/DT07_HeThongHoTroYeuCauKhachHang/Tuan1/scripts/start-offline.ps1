$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
if (-not (Test-Path '.env')) { throw 'Missing .env. Run .\scripts\prepare-offline.ps1 once while online.' }
docker compose up --no-build --pull never -d
if ($LASTEXITCODE -ne 0) { throw 'Could not start the locally cached application images.' }
docker compose ps
