[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$StartupTimeoutSeconds = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
$apiDll = Join-Path $solutionRoot "src\Submission.Api\bin\Debug\net10.0\Submission.Api.dll"
$clientDll = Join-Path $solutionRoot "src\Submission.Client\bin\Debug\net10.0\Submission.Client.dll"
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
            throw "Submission.Api exited during startup. $stderr"
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

    throw "Submission.Api did not start within $StartupTimeoutSeconds seconds."
}

$succeeded = $false
Push-Location $solutionRoot
try {
    $baseUri = [uri]$BaseUrl
    if (Test-TcpPort -Uri $baseUri) {
        throw "Port $($baseUri.Port) is already in use. Stop the existing service and retry."
    }

    & dotnet build .\RestfulClientServer.slnx --nologo
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

    $list = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/assignments"
    Assert-Equal 200 $list.Status "GET /api/assignments returns 200"
    $assignments = $list.Body | ConvertFrom-Json
    Assert-Equal 3 $assignments.Count "Assignment list contains 3 items"

    $detail = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/assignments/1"
    Assert-Equal 200 $detail.Status "GET /api/assignments/1 returns 200"

    $missing = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/assignments/999"
    Assert-Equal 404 $missing.Status "GET /api/assignments/999 returns 404"
    Assert-Equal "ASSIGNMENT_NOT_FOUND" (($missing.Body | ConvertFrom-Json).code) `
        "Missing assignment returns ApiError"

    $badRoute = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/assignments/abc"
    Assert-Equal 404 $badRoute.Status "GET /api/assignments/abc returns 404"
    Assert-True ([string]::IsNullOrWhiteSpace($badRoute.Body)) `
        "Invalid assignment id returns an empty body"

    $validBody = @{
        studentCode  = "SV900"
        repositoryUrl = "https://example.com/sv900/rest-api"
    } | ConvertTo-Json -Compress

    $unsupported = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/2/submissions" `
        -Body $validBody
    Assert-Equal 415 $unsupported.Status "Missing Content-Type returns 415"

    $malformed = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/2/submissions" `
        -Body '{"studentCode":"SV901",' `
        -ContentType "application/json"
    Assert-Equal 400 $malformed.Status "Malformed JSON returns 400"

    $closed = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/3/submissions" `
        -Body $validBody `
        -ContentType "application/json"
    Assert-Equal 400 $closed.Status "Closed assignment returns 400"
    Assert-Equal "ASSIGNMENT_CLOSED" (($closed.Body | ConvertFrom-Json).code) `
        "Closed assignment returns ApiError"

    $invalidBody = @{
        studentCode  = ""
        repositoryUrl = ""
    } | ConvertTo-Json -Compress
    $invalid = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/1/submissions" `
        -Body $invalidBody `
        -ContentType "application/json"
    Assert-Equal 400 $invalid.Status "Invalid submission returns 400"
    Assert-Equal "INVALID_SUBMISSION" (($invalid.Body | ConvertFrom-Json).code) `
        "Invalid submission returns ApiError"

    $missingParent = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/999/submissions" `
        -Body $validBody `
        -ContentType "application/json"
    Assert-Equal 404 $missingParent.Status "Submission to assignment 999 returns 404"
    Assert-Equal "ASSIGNMENT_NOT_FOUND" (($missingParent.Body | ConvertFrom-Json).code) `
        "Missing parent returns ApiError"

    $created = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/1/submissions" `
        -Body $validBody `
        -ContentType "application/json"
    Assert-Equal 201 $created.Status "Valid submission returns 201"
    Assert-True ($created.Location -match "^/api/submissions/[0-9a-f-]+$") `
        "Valid submission returns Location"
    $createdId = ($created.Body | ConvertFrom-Json).id

    $readBack = Invoke-HttpCase -Method GET -Uri "$BaseUrl/api/submissions/$createdId"
    Assert-Equal 200 $readBack.Status "GET created submission returns 200"
    Assert-Equal "SV900" (($readBack.Body | ConvertFrom-Json).studentCode) `
        "Read-back submission has the expected student code"

    $duplicateBody = @{
        studentCode  = "sv900"
        repositoryUrl = "https://example.com/sv900/rest-api-v2"
    } | ConvertTo-Json -Compress
    $duplicate = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/1/submissions" `
        -Body $duplicateBody `
        -ContentType "application/json"
    Assert-Equal 409 $duplicate.Status "Duplicate submission returns 409"
    Assert-Equal "DUPLICATE_SUBMISSION" (($duplicate.Body | ConvertFrom-Json).code) `
        "Duplicate submission returns ApiError"

    $deleted = Invoke-HttpCase -Method DELETE -Uri "$BaseUrl/api/submissions/$createdId"
    Assert-Equal 204 $deleted.Status "DELETE existing submission returns 204"

    $deletedAgain = Invoke-HttpCase -Method DELETE -Uri "$BaseUrl/api/submissions/$createdId"
    Assert-Equal 404 $deletedAgain.Status "DELETE removed submission returns 404"
    Assert-Equal "SUBMISSION_NOT_FOUND" (($deletedAgain.Body | ConvertFrom-Json).code) `
        "Second DELETE returns ApiError"

    $createdAgain = Invoke-HttpCase `
        -Method POST `
        -Uri "$BaseUrl/api/assignments/1/submissions" `
        -Body $validBody `
        -ContentType "application/json"
    Assert-Equal 201 $createdAgain.Status "Submission after DELETE returns 201"
    Assert-True ((($createdAgain.Body | ConvertFrom-Json).id) -ne $createdId) `
        "Submission after DELETE receives a new id"

    $clientOutput = (& dotnet $clientDll $BaseUrl 2>&1 | Out-String)
    Assert-Equal 0 $LASTEXITCODE "Submission.Client successful run exits with code 0"
    Assert-True ($clientOutput -match "Danh sach bai tap") "Client prints assignment list"
    Assert-True ($clientOutput -match "Da tao luot nop") "Client creates a submission"
    Assert-True ($clientOutput -match "Doc lai luot nop") "Client reads the submission"
    Assert-True ($clientOutput -match "INVALID_SUBMISSION") "Client reads ApiError"

    $secondClientOutput = (& dotnet $clientDll $BaseUrl 2>&1 | Out-String)
    Assert-Equal 0 $LASTEXITCODE "Second Submission.Client run exits with code 0"
    Assert-True ($secondClientOutput -match "DUPLICATE_SUBMISSION") `
        "Second client run reports duplicate submission"

    $unusedUrl = Get-UnusedLoopbackUrl
    $networkOutput = (& dotnet $clientDll $unusedUrl 2>&1 | Out-String)
    Assert-Equal 0 $LASTEXITCODE "Submission.Client network-error run exits with code 0"
    Assert-True ($networkOutput -match "Khong ket noi duoc API") `
        "Submission.Client handles connection failure"

    Write-Host "Submission integration tests passed: $script:passed assertions."
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
