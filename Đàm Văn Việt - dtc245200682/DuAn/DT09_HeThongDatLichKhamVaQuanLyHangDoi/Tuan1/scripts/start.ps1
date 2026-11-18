$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw 'Compose failed. Check Docker Desktop.' }
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    try {
        $ready = Invoke-RestMethod http://localhost:5080/health
        if ($ready.status -eq 'ready') {
            Write-Host 'Patient UI: http://localhost:5080/'
            Write-Host 'Staff UI: http://localhost:5080/staff.html'
            exit 0
        }
    } catch { }
    Start-Sleep -Seconds 2
}
throw 'API not ready. Check docker compose logs.'
