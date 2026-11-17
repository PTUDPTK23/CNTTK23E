[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5100",
    [int]$StartupTimeoutSeconds = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
$apiDll = Join-Path $solutionRoot "src\Catalog.Api\bin\Debug\net10.0\Catalog.Api.dll"
$clientDll = Join-Path $solutionRoot "src\Catalog.Client\bin\Debug\net10.0\Catalog.Client.dll"
$apiProcess = $null
$stdoutPath = [System.IO.Path]::GetTempFileName()
$stderrPath = [System.IO.Path]::GetTempFileName()
$script:passed = 0

function Assert-Equal {
    param(
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)]$Actual,
        [Parameter(Mandatory)][string]$Name
    )

    if ($Expected -ne $Actual) {
        throw "$Name - expected '$Expected', received '$Actual'."
    }

    $script:passed++
    Write-Host "[PASS] $Name"
}

function Assert-True {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Name
    )

    if (-not $Condition) {
        throw "$Name - condition was false."
    }

    $script:passed++
    Write-Host "[PASS] $Name"
}

function Test-TcpPort {
    param([Parameter(Mandatory)][uri]$Uri)

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $task = $client.ConnectAsync($Uri.Host, $Uri.Port)
        return $task.Wait(300) -and $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-UnusedLoopbackUrl {
    $listener = [System.Net.Sockets.TcpListener]::new(
        [System.Net.IPAddress]::Loopback,
        0)
    $listener.Start()
    try {
        $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
        return "http://localhost:$port"
    }
    finally {
        $listener.Stop()
    }
}

function Invoke-HttpCase {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Uri,
        [AllowNull()][string]$Body = $null,
        [AllowNull()][string]$ContentType = $null
    )

    $parameters = @{
        Method          = $Method
        Uri             = $Uri
        UseBasicParsing = $true
    }

    if ($PSBoundParameters.ContainsKey("Body") -and $null -ne $Body) {
        $parameters.Body = $Body
    }

    if ($ContentType) {
        $parameters.ContentType = $ContentType
    }

    try {
        $response = Invoke-WebRequest @parameters
        return [pscustomobject]@{
            Status   = [int]$response.StatusCode
            Body     = [string]$response.Content
            Location = [string]$response.Headers["Location"]
        }
    }
    catch {
        $responseProperty = $_.Exception.PSObject.Properties["Response"]
        if ($null -eq $responseProperty -or $null -eq $responseProperty.Value) {
            throw
        }
        $response = $responseProperty.Value

        $reader = [System.IO.StreamReader]::new($response.GetResponseStream())
        try {
            $responseBody = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        return [pscustomobject]@{
            Status   = [int]$response.StatusCode
            Body     = [string]$responseBody
            Location = [string]$response.Headers["Location"]
        }
    }
}

function Wait-ForApi {
    param(
        [Parameter(Mandatory)][string]$HealthUrl,
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            $stderr = Get-Content -Raw -LiteralPath $stderrPath -ErrorAction SilentlyContinue
            throw "Catalog.Api exited during startup. $stderr"
        }

        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $HealthUrl -TimeoutSec 1
            if ([int]$response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Catalog.Api did not start within $StartupTimeoutSeconds seconds."
}

$succeeded = $false
Push-Location $solutionRoot
try {
    $baseUri = [uri]$BaseUrl
    if (Test-TcpPort -Uri $baseUri) {
        throw "Port $($baseUri.Port) is already in use. Stop the existing service and retry."
    }

    & dotnet build .\CatalogApi.slnx --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    $apiProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("`"$apiDll`"", "--urls", $BaseUrl) `
        -WorkingDirectory $solutionRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru

    Wait-ForApi -HealthUrl "$BaseUrl/" -Process $apiProcess

    $health = Invoke-HttpCase -Method GET -Uri "$BaseUrl/"
    Assert-Equal 200 $health.Status "GET / returns 200"

    $list = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/products"
    Assert-Equal 200 $list.Status "GET /api/products returns 200"
    $products = $list.Body | ConvertFrom-Json
    Assert-Equal 3 $products.Count "Initial product list contains 3 items"

    $detail = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/products/1"
    Assert-Equal 200 $detail.Status "GET /api/products/1 returns 200"

    $missing = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/products/999"
    Assert-Equal 404 $missing.Status "GET /api/products/999 returns 404"
    Assert-Equal "PRODUCT_NOT_FOUND" (($missing.Body | ConvertFrom-Json).code) `
        "Missing product returns ApiError"

    $badRoute = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/products/abc"
    Assert-Equal 404 $badRoute.Status "GET /api/products/abc returns 404"
    Assert-True ([string]::IsNullOrWhiteSpace($badRoute.Body)) `
        "Invalid route id returns an empty body"

    $validBody = @{
        name        = "Lap trinh gRPC"
        description = "Tai lieu bai 5"
        price       = 80000
    } | ConvertTo-Json -Compress
    $created = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/products" `
        -Body $validBody `
        -ContentType "application/json"
    Assert-Equal 201 $created.Status "Valid POST returns 201"
    Assert-True ($created.Location -match "^/api/products/\d+$") `
        "Valid POST returns Location"

    $invalidBody = @{
        name        = "San pham loi"
        description = ""
        price       = -1
    } | ConvertTo-Json -Compress
    $invalid = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/products" `
        -Body $invalidBody `
        -ContentType "application/json"
    Assert-Equal 400 $invalid.Status "Negative price returns 400"
    Assert-Equal "INVALID_PRODUCT" (($invalid.Body | ConvertFrom-Json).code) `
        "Negative price returns ApiError"

    $unsupported = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/products" `
        -Body $validBody
    Assert-Equal 415 $unsupported.Status "Missing Content-Type returns 415"

    $clientOutput = (& dotnet $clientDll $BaseUrl 2>&1 | Out-String)
    Assert-Equal 0 $LASTEXITCODE "Catalog.Client successful run exits with code 0"
    Assert-True ($clientOutput -match "Danh sach san pham") "Client prints product list"
    Assert-True ($clientOutput -match "Da tao san pham moi") "Client creates a product"
    Assert-True ($clientOutput -match "Chi tiet san pham 1") "Client reads product detail"
    Assert-True ($clientOutput -match "INVALID_PRODUCT") "Client reads ApiError"

    $unusedUrl = Get-UnusedLoopbackUrl
    $networkOutput = (& dotnet $clientDll $unusedUrl 2>&1 | Out-String)
    Assert-Equal 0 $LASTEXITCODE "Catalog.Client network-error run exits with code 0"
    Assert-True ($networkOutput -match "Khong ket noi duoc Catalog API") `
        "Catalog.Client handles connection failure"

    Write-Host "Catalog integration tests passed: $script:passed assertions."
    $succeeded = $true
}
catch {
    [Console]::Error.WriteLine("[FAIL] $($_.Exception.Message)")
    [Console]::Error.WriteLine($_.ScriptStackTrace)
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    Pop-Location
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
}

if (-not $succeeded) {
    exit 1
}
