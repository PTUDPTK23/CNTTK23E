$ErrorActionPreference = "Stop"
$root = "D:\HE_THONG_QUAN_LI_THU_VIEN_VA_DAT_MUON_SACH_PHAN_TAN"
Set-Location $root

Write-Host "`n===== LIBRARY MANAGEMENT SYSTEM =====" -ForegroundColor Cyan

# Kiem tra Docker
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Desktop chua san sang. Hay mo Docker Desktop roi chay lai."
}

# Chi khoi dong 2 container cua du an thu vien
foreach ($name in @("library-postgres", "library-rabbitmq")) {
    $status = docker inspect -f '{{.State.Running}}' $name 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Khong tim thay container $name. Khong tu tao container moi."
    }
    if ($status -ne "true") {
        Write-Host "Dang khoi dong $name..." -ForegroundColor Yellow
        docker start $name
        if ($LASTEXITCODE -ne 0) { throw "Khong khoi dong duoc $name" }
    } else {
        Write-Host "$name dang chay." -ForegroundColor Green
    }
}

# Cho database va RabbitMQ san sang
foreach ($port in @(5433, 5672)) {
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        $test = Test-NetConnection 127.0.0.1 -Port $port -WarningAction SilentlyContinue
        if ($test.TcpTestSucceeded) { $ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (!$ready) { throw "Dich vu tai cong $port chua san sang." }
    Write-Host "Cong $port san sang." -ForegroundColor Green
}

# Kiem tra cong ung dung
foreach ($port in @(8081, 5173)) {
    $used = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($used) {
        Write-Host "Cong $port dang duoc su dung. Bo qua mo them tien trinh." -ForegroundColor Yellow
    }
}

$apiPort = Get-NetTCPConnection -LocalPort 8081 -State Listen -ErrorAction SilentlyContinue
if (!$apiPort) {
    Start-Process powershell.exe -WorkingDirectory $root -ArgumentList "-NoExit -ExecutionPolicy Bypass -File `"$root\scripts\run-api.ps1`""
    Write-Host "Da mo cua so Backend API." -ForegroundColor Green
}

$workerDir = Join-Path $root "backend\reservation-worker"
if (!(Test-Path (Join-Path $workerDir "pom.xml"))) {
    throw "Khong tim thay pom.xml cua Reservation Worker."
}
Start-Process powershell.exe -WorkingDirectory $root -ArgumentList "-NoExit -ExecutionPolicy Bypass -File `"$root\scripts\run-worker.ps1`""
Write-Host "Da mo cua so Reservation Worker." -ForegroundColor Green

$frontPort = Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue
if (!$frontPort) {
    if (!(Test-Path (Join-Path $root "frontend\node_modules"))) {
        throw "Frontend chua co node_modules. Hay chay npm install trong frontend."
    }
    Start-Process powershell.exe -WorkingDirectory (Join-Path $root "frontend") -ArgumentList "-NoExit -Command `"npm run dev -- --host 127.0.0.1`""
    Write-Host "Da mo cua so Frontend." -ForegroundColor Green
}

Write-Host "`n===== HE THONG DANG KHOI DONG =====" -ForegroundColor Green
Write-Host "Frontend: http://localhost:5173"
Write-Host "Backend:  http://localhost:8081"
Write-Host "Health:   http://localhost:8081/actuator/health"
Write-Host "RabbitMQ: http://localhost:15672"
Write-Host "`nKiem tra log trong cac cua so PowerShell vua mo." -ForegroundColor Yellow