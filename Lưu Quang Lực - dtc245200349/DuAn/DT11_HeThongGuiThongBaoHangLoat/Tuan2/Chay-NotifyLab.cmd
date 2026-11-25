@echo off
setlocal EnableExtensions DisableDelayedExpansion
title NotifyLab
pushd "%~dp0"
if errorlevel 1 goto :folder_error

if not exist "docker-compose.yml" (
    echo Khong tim thay docker-compose.yml. Hay de file nay trong thu muc NotifyLab.
    goto :failed
)

where docker.exe >nul 2>&1
if errorlevel 1 (
    echo Khong tim thay Docker. Hay cai Docker Desktop va thu lai.
    goto :failed
)

docker info --format "{{.OSType}}" >nul 2>&1
if errorlevel 1 (
    echo Dang khoi dong Docker Desktop...
    docker desktop start --detach
)

echo Dang doi Docker Engine san sang...
powershell.exe -NoProfile -Command "$deadline = [DateTime]::UtcNow.AddSeconds(180); do { $type = (& docker info --format '{{.OSType}}' 2>$null | Out-String).Trim(); if ($LASTEXITCODE -eq 0) { if ($type -eq 'linux') { exit 0 }; if ($type -eq 'windows') { exit 2 } }; Start-Sleep -Seconds 2 } while ([DateTime]::UtcNow -lt $deadline); exit 3"
if errorlevel 3 (
    echo Docker Engine chua phan hoi sau 3 phut. Hay kiem tra Docker Desktop.
    goto :failed
)
if errorlevel 2 (
    echo Docker dang dung Windows containers. Hay chuyen sang Linux containers.
    goto :failed
)
if errorlevel 1 (
    echo Khong kiem tra duoc Docker Engine.
    goto :failed
)

rem Keep the delivery mode of existing Compose containers, including stopped ones.
set "EXISTING_CONTAINERS="
set "SMTP_OVERRIDE="
for /f %%I in ('docker compose -f docker-compose.yml ps --all --quiet campaign-api notification-worker 2^>nul') do (
    set "EXISTING_CONTAINERS=1"
    docker inspect --format "{{json .Config.Labels}}" %%I 2>nul | findstr /i /l /c:"compose.smtp.yml" >nul
    if not errorlevel 1 set "SMTP_OVERRIDE=-f compose.smtp.yml"
)

rem A fresh Compose stack can reuse the SMTP settings saved in the local .env.
if not defined EXISTING_CONTAINERS if exist ".env" (
    docker compose -f docker-compose.yml -f compose.smtp.yml config --quiet >nul 2>&1
    if not errorlevel 1 set "SMTP_OVERRIDE=-f compose.smtp.yml"
)

if defined SMTP_OVERRIDE (
    echo Che do gui: SMTP that.
) else (
    echo Che do gui: Mailpit.
)
echo Dang build va khoi dong NotifyLab...
docker compose -f docker-compose.yml %SMTP_OVERRIDE% up -d --build --scale notification-worker=2 --wait --wait-timeout 120
if errorlevel 1 goto :failed

set "APP_ADDRESS="
for /f "delims=" %%A in ('docker compose -f docker-compose.yml %SMTP_OVERRIDE% port campaign-api 8080') do set "APP_ADDRESS=%%A"
if not defined APP_ADDRESS (
    echo Khong tim thay cong cua NotifyLab.
    goto :failed
)
set "APP_URL=http://%APP_ADDRESS%"

set "MAILPIT_ADDRESS="
for /f "delims=" %%A in ('docker compose -f docker-compose.yml %SMTP_OVERRIDE% port mailpit 8025') do set "MAILPIT_ADDRESS=%%A"

echo Dang doi API ket noi PostgreSQL...
powershell.exe -NoProfile -Command "for ($attempt = 0; $attempt -lt 30; $attempt++) { try { $state = Invoke-RestMethod -Uri ($env:APP_URL + '/health/ready') -TimeoutSec 2; if ($state.status -eq 'ready') { exit 0 } } catch {} ; Start-Sleep -Seconds 2 }; exit 1"
if errorlevel 1 (
    echo API chua san sang. Kiem tra log cua campaign-api trong Docker Desktop.
    goto :failed
)

echo NotifyLab da san sang tai %APP_URL%
set "BROWSER_FAILED="
if defined MAILPIT_ADDRESS (
    call :open_mailpit
    if errorlevel 1 set "BROWSER_FAILED=1"
) else (
    echo Khong tim thay cong cua Mailpit.
    set "BROWSER_FAILED=1"
)
start "" "%APP_URL%"
if errorlevel 1 (
    echo Khong mo duoc trang NotifyLab. Hay sao chep dia chi o tren.
    set "BROWSER_FAILED=1"
)
if defined BROWSER_FAILED (
    goto :failed
)
popd
exit /b 0

:open_mailpit
set "MAILPIT_URL=http://%MAILPIT_ADDRESS%"
echo Mailpit da san sang tai %MAILPIT_URL%
start "" "%MAILPIT_URL%"
if errorlevel 1 (
    echo Khong mo duoc trang Mailpit. Hay sao chep dia chi o tren.
    exit /b 1
)
exit /b 0

:failed
echo.
echo Xem thong bao o tren de biet cach xu ly.
popd
pause
exit /b 1

:folder_error
echo Khong mo duoc thu muc NotifyLab.
pause
exit /b 1
