# EventFlow - Stop Local Services Script
$baseDir = $PSScriptRoot
$pidFile = "$baseDir\.local-pids.txt"

if (Test-Path $pidFile) {
    $pids = (Get-Content $pidFile).Split(',')
    foreach ($p in $pids) {
        if (![string]::IsNullOrWhiteSpace($p)) {
            try {
                Stop-Process -Id ([int]$p.Trim()) -Force -ErrorAction SilentlyContinue
                Write-Host "Stopped process ID $p" -ForegroundColor Green
            } catch { }
        }
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
    Write-Host "All local EventFlow services stopped." -ForegroundColor Cyan
} else {
    Write-Host "No running PID file found." -ForegroundColor Yellow
}
