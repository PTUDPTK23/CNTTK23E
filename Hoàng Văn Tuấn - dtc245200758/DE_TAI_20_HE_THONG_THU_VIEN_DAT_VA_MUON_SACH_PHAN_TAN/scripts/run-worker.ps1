$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location (Join-Path $root "backend\reservation-worker")

$envFile = Join-Path $root ".env.local"
if (!(Test-Path $envFile)) { throw "Khong tim thay .env.local" }

Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    if ($line -and !$line.StartsWith("#") -and $line.Contains("=")) {
        $parts = $line -split "=", 2
        $name = $parts[0].Trim()
        $value = $parts[1].Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

Write-Host "Dang khoi dong Reservation Worker..." -ForegroundColor Cyan
mvn "-Dspring-boot.run.jvmArguments=-Duser.timezone=Asia/Ho_Chi_Minh" spring-boot:run