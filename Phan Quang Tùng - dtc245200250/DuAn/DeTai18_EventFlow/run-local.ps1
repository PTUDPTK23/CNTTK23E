# EventFlow - Local Development Orchestrator Script
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Starting EventFlow Distributed System locally (.NET 10) " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$baseDir = $PSScriptRoot

Write-Host "`n[1/4] Starting EventApi on http://localhost:5000..." -ForegroundColor Green
$apiProc = Start-Process dotnet -ArgumentList "run --project `"$baseDir\src\EventFlow.EventApi`" --urls http://localhost:5000" -PassThru

Start-Sleep -Seconds 3

Write-Host "[2/4] Starting DashboardService on http://localhost:5001..." -ForegroundColor Green
$dashProc = Start-Process dotnet -ArgumentList "run --project `"$baseDir\src\EventFlow.DashboardService`" --urls http://localhost:5001" -PassThru

Start-Sleep -Seconds 2

Write-Host "[3/4] Starting AuditWorker on http://localhost:5002..." -ForegroundColor Green
$auditProc = Start-Process dotnet -ArgumentList "run --project `"$baseDir\src\EventFlow.AuditWorker`" --urls http://localhost:5002" -PassThru

Start-Sleep -Seconds 2

Write-Host "[4/4] Starting WebClient (Blazor) on http://localhost:5003..." -ForegroundColor Green
$webProc = Start-Process dotnet -ArgumentList "run --project `"$baseDir\src\EventFlow.WebClient`" --urls http://localhost:5003" -PassThru

Write-Host "`nAll 4 services have been launched!" -ForegroundColor Cyan
Write-Host "  - Event API Swagger:      http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "  - Dashboard Hub & Stats:  http://localhost:5001/hubs/dashboard" -ForegroundColor Yellow
Write-Host "  - Audit Worker REST:      http://localhost:5002/api/audit/logs" -ForegroundColor Yellow
Write-Host "  - Web Client (SaaS UI):   http://localhost:5003" -ForegroundColor Yellow
Write-Host "`nPress Ctrl+C or run .\stop-local.ps1 to stop all services." -ForegroundColor White

# Save PIDs
Set-Content -Path "$baseDir\.local-pids.txt" -Value "$($apiProc.Id),$($dashProc.Id),$($auditProc.Id),$($webProc.Id)"
