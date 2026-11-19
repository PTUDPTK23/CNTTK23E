# Chạy unit test của cả hai backend (không cần RabbitMQ/PostgreSQL).
$root = Split-Path -Parent $PSScriptRoot
foreach ($m in 'library-api','reservation-worker') {
  Push-Location (Join-Path $root "backend\$m")
  if (Test-Path .\mvnw.cmd) { .\mvnw.cmd test } else { mvn test }
  Pop-Location
}
