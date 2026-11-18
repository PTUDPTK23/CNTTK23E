$baseUrl = "http://localhost:5000"
$outputFile = Join-Path $PSScriptRoot "results.txt"

$results = [System.Collections.Generic.List[string]]::new()
$script:failed = 0

function Test-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [int]$Expected,
        [string]$Body = ""
    )

    $actual = 0
    $content = ""

    try {
        $params = @{
            Uri = "$baseUrl$Path"
            Method = $Method
            UseBasicParsing = $true
            ErrorAction = "Stop"
        }

        if ($Method -eq "POST") {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }

        $response = Invoke-WebRequest @params
        $actual = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        $response = $_.Exception.Response

        if ($null -ne $response) {
            $actual = [int]$response.StatusCode

            try {
                if ($response.Content) {
                    $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                }
                else {
                    $stream = $response.GetResponseStream()
                    if ($stream) {
                        $reader = New-Object System.IO.StreamReader($stream)
                        $content = $reader.ReadToEnd()
                        $reader.Dispose()
                    }
                }
            }
            catch {
                $content = ""
            }
        }
        else {
            $actual = 0
            $content = $_.Exception.Message
        }
    }

    $passed = ($actual -eq $Expected)

    if (-not $passed) {
        $script:failed++
    }

    $status = if ($passed) { "PASS" } else { "FAIL" }
    $line = "$status | $Name | Expected: $Expected | Actual: $actual"

    $results.Add($line)
    Write-Host $line -ForegroundColor $(if ($passed) { "Green" } else { "Red" })

    return $content
}

Write-Host "===== EDUSUBMIT HTTP VERIFICATION =====" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl"
Write-Host ""

# 1. Danh sach bai tap
Test-Api "GET danh sach bai tap" "GET" "/api/assignments" 200 | Out-Null

# 2. Chi tiet bai tap ton tai
Test-Api "GET bai tap ID 1" "GET" "/api/assignments/1" 200 | Out-Null

# 3. Bai tap khong ton tai
Test-Api "GET bai tap ID 999" "GET" "/api/assignments/999" 404 | Out-Null

# 4. Nop bai hop le
$body = @{
    studentCode = "dtc245200758"
    repositoryUrl = "https://github.com/dtc245200758-maker/CNTTK23E"
} | ConvertTo-Json

$created = Test-Api "POST nop bai hop le" "POST" "/api/assignments/1/submissions" 201 $body

# 5. Doc lai luot nop
if ($created) {
    try {
        $submission = $created | ConvertFrom-Json

        if ($submission.id) {
            Test-Api "GET doc lai luot nop" "GET" "/api/submissions/$($submission.id)" 200 | Out-Null
        }
        else {
            $script:failed++
            $results.Add("FAIL | Khong tim thay ID trong ket qua POST")
        }
    }
    catch {
        $script:failed++
        $results.Add("FAIL | Khong doc duoc JSON tu ket qua POST")
    }
}

# 6. Nop bai sai du lieu
$invalidBody = @{
    studentCode = ""
    repositoryUrl = ""
} | ConvertTo-Json

Test-Api "POST du lieu khong hop le" "POST" "/api/assignments/1/submissions" 400 $invalidBody | Out-Null

# 7. Submission khong ton tai
$fakeId = "00000000-0000-0000-0000-000000000001"
Test-Api "GET submission khong ton tai" "GET" "/api/submissions/$fakeId" 404 | Out-Null

$results.Add("")
$results.Add("Tong so loi: $script:failed")
$results.Add("Thoi gian: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")

$results | Set-Content -Path $outputFile -Encoding UTF8

Write-Host ""
Write-Host "===== TONG KET =====" -ForegroundColor Cyan
Write-Host "So loi: $script:failed"
Write-Host "File ket qua: $outputFile"

if ($script:failed -eq 0) {
    Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
}
else {
    Write-Host "CO TEST THAT BAI. CAN KIEM TRA LAI." -ForegroundColor Red
}
