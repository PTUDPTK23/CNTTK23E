param(
    [int]$WebPort = 8088,
    [int]$ApiPort = 5000,
    [int]$NotificationPort = 5001
)

$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $checks = @(
        "http://localhost:$ApiPort/health",
        "http://localhost:$ApiPort/health/live",
        "http://localhost:$NotificationPort/health",
        "http://localhost:$WebPort/health",
        "http://localhost:$WebPort/health/api",
        "http://localhost:$WebPort/health/notification",
        "http://localhost:$WebPort/"
    )
    foreach ($url in $checks) {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 15
        if ($response.StatusCode -ne 200) { throw "Unexpected HTTP status: $url" }
        Write-Output "PASS HTTP 200 $url"
    }
    $system = Invoke-RestMethod "http://localhost:$WebPort/api/system"
    if ($system.name -ne 'ClinicQueue' -or $system.milestone -ne 'M0') {
        throw 'Unexpected API system metadata.'
    }
    $containers = docker compose ps -a --format json
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect ClinicQueue containers.' }
    $rows = @($containers | ForEach-Object { $_ | ConvertFrom-Json })
    foreach ($service in @('sqlserver', 'rabbitmq', 'api', 'worker', 'notification', 'web')) {
        $container = $rows | Where-Object Service -eq $service
        if (-not $container -or $container.Health -ne 'healthy') {
            throw "Service is not healthy: $service"
        }
        Write-Output "PASS healthy $service"
    }
    foreach ($service in @('api-migrate', 'worker-migrate')) {
        $container = $rows | Where-Object Service -eq $service
        if (-not $container -or $container.State -ne 'exited' -or $container.ExitCode -ne 0) {
            throw "Migration did not complete successfully: $service"
        }
        Write-Output "PASS migration $service"
    }
}
finally { Pop-Location }
