$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
$runDirectory = Join-Path $projectRoot '.work\verification'
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$servers = @()

function Build-Solution([string]$name) {
    Write-Host "PS $projectRoot> dotnet build $name/$name.slnx -m:1"
    & dotnet build "$name/$name.slnx" -m:1 --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build $name failed" }
}

function Assert-Equal($actual, $expected, [string]$label) {
    if ($actual -ne $expected) { throw "$label expected=$expected actual=$actual" }
    Write-Host "PASS $label => $actual" -ForegroundColor Green
}

function Invoke-Http([string]$path, [string]$method = 'GET', $data = $null, [int]$port = 5000) {
    $bodyPath = Join-Path $runDirectory 'response.json'
    $headersPath = Join-Path $runDirectory 'headers.txt'
    $curlArgs = @('--silent', '--show-error', '--max-time', '10', '--request', $method,
        '--dump-header', $headersPath, '--output', $bodyPath, '--write-out', '%{http_code}',
        "http://localhost:$port$path")
    if ($null -ne $data) {
        $payloadPath = Join-Path $runDirectory 'payload.json'
        $payload = ConvertTo-Json -InputObject $data -Compress
        [IO.File]::WriteAllText($payloadPath, $payload, (New-Object Text.UTF8Encoding($false)))
        $curlArgs += @('--header', 'Content-Type: application/json', '--data-binary', "@$payloadPath")
    }
    $status = & curl.exe @curlArgs
    if ($LASTEXITCODE -ne 0) { throw "curl failed: $method $path" }
    [pscustomobject]@{
        Status = [int]$status
        Body = [IO.File]::ReadAllText($bodyPath)
        Headers = [IO.File]::ReadAllText($headersPath)
    }
}

function Start-Api([string]$solution, [string]$project, [int]$port) {
    $probe = New-Object Net.Sockets.TcpClient
    try {
        $attempt = $probe.ConnectAsync('localhost', $port)
        if ($attempt.Wait(400) -and $probe.Connected) { throw "Port $port is in use. Stop the existing API first." }
    } catch [AggregateException] { } finally { $probe.Dispose() }
    $assembly = Join-Path $projectRoot "$solution\src\$project\bin\Debug\net10.0\$project.dll"
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @("`"$assembly`"", '--urls', "http://localhost:$port") `
        -WorkingDirectory (Join-Path $projectRoot "$solution\src\$project") -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory "$project-out.txt") `
        -RedirectStandardError (Join-Path $runDirectory "$project-error.txt")
    $script:servers += $process
    for ($index = 0; $index -lt 60; $index++) {
        if ($process.HasExited) { throw "API $project exited. See .work/verification logs." }
        $probe = New-Object Net.Sockets.TcpClient
        try {
            if ($probe.ConnectAsync('localhost', $port).Wait(200) -and $probe.Connected) { return }
        } catch [AggregateException] { } finally { $probe.Dispose() }
        Start-Sleep -Milliseconds 200
    }
    throw "API $project did not start on port $port"
}

try {
    Build-Solution 'PingSignalR'
    Build-Solution 'RealtimeSubmissionSystem'
    Start-Api 'PingSignalR' 'Ping.Api' 5200
    Start-Api 'RealtimeSubmissionSystem' 'Submission.RealtimeApi' 5000

    Assert-Equal (Invoke-Http '/').Status 200 'GET root'
    Assert-Equal (Invoke-Http '/api/assignments').Status 200 'GET assignments'
    $created = Invoke-Http '/api/assignments/1/submissions' 'POST' @{
        studentCode = 'dtc245200349'; repositoryUrl = 'https://example.com/dtc245200349/signalr'
    }
    Assert-Equal $created.Status 201 'POST submission'
    $submission = $created.Body | ConvertFrom-Json
    Assert-Equal $submission.Status 'received' 'initial status'
    $location = "/api/submissions/$($submission.id)"
    if (-not $created.Headers.Contains("Location: $location")) { throw 'Missing or incorrect Location' }
    Write-Host "PASS Location: $location"
    Assert-Equal (Invoke-Http '/api/assignments/1/submissions' 'POST' @{}).Status 400 'missing fields'
    Assert-Equal (Invoke-Http '/api/assignments/999/submissions' 'POST' @{}).Status 404 'unknown assignment'
    Assert-Equal (Invoke-Http '/api/submissions/00000000-0000-0000-0000-000000000000').Status 404 'unknown submission'
    $negotiate = Invoke-Http '/hubs/submissions/negotiate?negotiateVersion=1' 'POST'
    Assert-Equal $negotiate.Status 200 'EduSubmit negotiate'
    if (-not ($negotiate.Body | ConvertFrom-Json).connectionId) { throw 'Missing connectionId' }
    Assert-Equal (Invoke-Http '/hub/submissions/negotiate?negotiateVersion=1' 'POST').Status 404 'wrong hub'
    Assert-Equal (Invoke-Http '/api/assignments/3/submissions' 'POST' @{}).Status 400 'closed assignment'
    Start-Sleep -Seconds 5
    $latest = Invoke-Http $location
    Assert-Equal $latest.Status 200 'GET created submission'
    Assert-Equal ($latest.Body | ConvertFrom-Json).Status 'graded' 'final REST status'
    $ping = Invoke-Http '/hubs/ping/negotiate?negotiateVersion=1' 'POST' $null 5200
    Assert-Equal $ping.Status 200 'Ping negotiate'
    if (-not ($ping.Body | ConvertFrom-Json).connectionId) { throw 'Missing Ping connectionId' }

    $client = Join-Path $projectRoot 'RealtimeSubmissionSystem\src\Submission.RealtimeClient\bin\Debug\net10.0\Submission.RealtimeClient.dll'
    $result = & dotnet $client 'http://localhost:5000' 'SV001' 2>&1
    $clientCode = $LASTEXITCODE
    $result | ForEach-Object { Write-Host $_ }
    Assert-Equal $clientCode 0 'client exit'
    if (($result -join "`n") -notmatch 'Thong bao SignalR: .* -> graded') { throw 'Missing graded push' }
    Write-Host 'PASS graded received over SignalR'

    $result = & dotnet $client 'http://localhost:5000' 'SV001' '--count' '2' '--second-repository' 'http://example.com/invalid' 2>&1
    $clientCode = $LASTEXITCODE
    $result | ForEach-Object { Write-Host $_ }
    Assert-Equal $clientCode 2 'two submissions with one failed: expected exit'
    if (($result -join "`n") -notmatch '!!! CANH BAO:' -or ($result -join "`n") -notmatch '-> graded') {
        throw 'Missing graded or failed notification'
    }
    Write-Host 'ALL CHECKS PASSED' -ForegroundColor Green
} finally {
    foreach ($process in $servers) {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -ErrorAction SilentlyContinue }
    }
}
