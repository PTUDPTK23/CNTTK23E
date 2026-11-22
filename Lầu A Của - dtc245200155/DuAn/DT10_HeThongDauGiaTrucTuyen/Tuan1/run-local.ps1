# Script chạy nhanh toàn bộ các service trên máy cục bộ (Windows PowerShell)
# Yêu cầu: Đã cài .NET 10 SDK (hoặc Docker nếu muốn chạy RabbitMQ)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "    KHỞI CHẠY HỆ THỐNG ĐẤU GIÁ TRỰC TUYẾN - LOCAL DEV            " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$root = $PSScriptRoot

# 1. Khởi động Bid Validation Service (gRPC - Port 5002)
Write-Host "[1/5] Khởi động Bid Validation Service (gRPC :5002)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/Services/BidValidation.Service'; `$env:PORT=5002; dotnet run"

Start-Sleep -Seconds 2

# 2. Khởi động Auction Hub (SignalR - Port 5003)
Write-Host "[2/5] Khởi động Auction Hub (SignalR :5003)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/Services/Auction.Hub'; `$env:PORT=5003; dotnet run"

Start-Sleep -Seconds 2

# 3. Khởi động Audit Worker (Audit Service - Port 5004)
Write-Host "[3/5] Khởi động Audit Worker (:5004)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/Services/Audit.Worker'; `$env:PORT=5004; dotnet run"

Start-Sleep -Seconds 2

# 4. Khởi động Auction API (REST Minimal API - Port 5001)
Write-Host "[4/5] Khởi động Auction API (REST :5001)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/Services/Auction.API'; `$env:PORT=5001; dotnet run"

Start-Sleep -Seconds 3

# 5. Khởi động Web Client (Giao diện Web - Port 5000)
Write-Host "[5/5] Khởi động Auction Web Client (:5000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/Clients/Auction.WebClient'; `$env:PORT=5000; dotnet run"

Write-Host "`nĐã khởi động tất cả các service!" -ForegroundColor Green
Write-Host "- Web Client: http://localhost:5000" -ForegroundColor Cyan
Write-Host "- Auction API Swagger: http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "- Bid Validation gRPC: http://localhost:5002" -ForegroundColor Cyan
Write-Host "- SignalR Hub: http://localhost:5003/hubs/auction" -ForegroundColor Cyan
Write-Host "- Audit Worker Logs: http://localhost:5004/api/audit-logs" -ForegroundColor Cyan
