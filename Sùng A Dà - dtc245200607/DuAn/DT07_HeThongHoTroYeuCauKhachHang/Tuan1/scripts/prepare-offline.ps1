$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
if (-not (Test-Path '.env')) {
    Copy-Item '.env.example' '.env'
    Write-Host 'Created .env from .env.example. Review the demo accounts before presenting.'
}
docker compose pull postgres rabbitmq
if ($LASTEXITCODE -ne 0) { throw 'Could not download the database or RabbitMQ image.' }
docker compose build
if ($LASTEXITCODE -ne 0) { throw 'Could not build the application images.' }
Write-Host 'Offline preparation complete. Images are cached locally.'
Write-Host 'Disconnect the network, then run: .\scripts\start-offline.ps1'
