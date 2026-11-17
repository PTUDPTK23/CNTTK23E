param([string]$BaseUrl = 'http://localhost:5000')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$http = [System.Net.Http.HttpClient]::new()
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('EduSubmit - dtc245200107 | HTTP verification')
$lines.Add('Run: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'))
$lines.Add('Base URL: ' + $BaseUrl)
function Send-Case($name, $method, $path, $body, $type, $expected) {
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($method), "$BaseUrl$path")
    if ($null -ne $body) {
        $req.Content = [System.Net.Http.StringContent]::new($body)
        $req.Content.Headers.ContentType = $null
        if ($type) { $req.Content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new($type) }
    }
    $res = $http.SendAsync($req).GetAwaiter().GetResult()
    $text = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $status = [int]$res.StatusCode
    $lines.Add("$name | $method $path | expected=$expected actual=$status")
    if ($res.Content.Headers.Allow.Count -gt 0) { $lines.Add('Allow: ' + ($res.Content.Headers.Allow -join ', ')) }
    if ($res.Headers.Location) { $lines.Add('Location: ' + $res.Headers.Location) }
    if ($text) { $lines.Add($text) }
    if ($status -ne $expected) { throw "$name failed: $status" }
    return $text
}
try {
    $null = Send-Case 'List' GET '/api/assignments' $null $null 200
    $code = 'verify-' + [Guid]::NewGuid().ToString('N')
    $body = @{studentCode=$code; repositoryUrl='https://github.com/hoangthikieuanh5-debug/CNTTK23E'} | ConvertTo-Json -Compress
    $created = (Send-Case '5a' POST '/api/assignments/1/submissions' $body 'application/json' 201) | ConvertFrom-Json
    $id = $created.id
    $null = Send-Case '5b' POST '/api/assignments/1/submissions' '{"studentCode":"","repositoryUrl":""}' 'application/json' 400
    $null = Send-Case '5c' POST '/api/assignments/999/submissions' $body 'application/json' 404
    $null = Send-Case '5d' POST '/api/assignments/1/submissions' $body $null 415
    $null = Send-Case '5e' POST '/api/assignments/1/submissions' '{"studentCode":"SV001",' 'application/json' 400
    $null = Send-Case '5f' POST '/api/assignments/1/submissions' '{"studentCode":"SV001"}' 'application/json' 400
    $null = Send-Case '5g' PUT '/api/assignments/1' $null $null 405
    $null = Send-Case '5h' POST '/api/assignments/2/submissions' $body 'application/json' 400
    $null = Send-Case '6a read created' GET "/api/submissions/$id" $null $null 200
    $null = Send-Case '6b missing submission' GET '/api/submissions/00000000-0000-0000-0000-000000000000' $null $null 404
    $null = Send-Case '4a' GET '/api/assignments/1' $null $null 200
    $null = Send-Case '4b' GET '/api/assignments/999' $null $null 404
    $null = Send-Case '4c' GET '/api/assignments/abc' $null $null 404
    $null = Send-Case '9a closed assignment 3' POST '/api/assignments/3/submissions' $body 'application/json' 400
    $duplicateBody = @{studentCode=('  ' + $code.ToUpperInvariant() + '  '); repositoryUrl='https://example.com/repo'} | ConvertTo-Json -Compress
    $null = Send-Case '9c duplicate (case and spaces)' POST '/api/assignments/1/submissions' $duplicateBody 'application/json' 409
    $null = Send-Case '9d delete' DELETE "/api/submissions/$id" $null $null 204
    $null = Send-Case '9e repeat delete' DELETE "/api/submissions/$id" $null $null 404
    $null = Send-Case 'Read after delete' GET "/api/submissions/$id" $null $null 404
    $again = (Send-Case '9f resubmit' POST '/api/assignments/1/submissions' $body 'application/json' 201) | ConvertFrom-Json
    if ($again.id -eq $id) { throw 'Resubmission must have a new ID' }
    $null = Send-Case 'Cleanup' DELETE ("/api/submissions/" + $again.id) $null $null 204
    $lines.Add('PASS: all HTTP status assertions matched.')
    $lines | Set-Content -Encoding UTF8 "$PSScriptRoot/results.txt"
    # Render actual captured output as a PNG; this is a log image, not an IDE screenshot.
    Add-Type -AssemblyName System.Drawing
    $font = [System.Drawing.Font]::new('Consolas', 12)
    $wrapped = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        for ($i=0; $i -lt $line.Length; $i+=115) { $wrapped.Add($line.Substring($i, [Math]::Min(115, $line.Length-$i))) }
    }
    $bitmap = [System.Drawing.Bitmap]::new(1500, ($wrapped.Count * 25 + 40))
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(22,27,34))
        $y = 20
        foreach ($line in $wrapped) { $graphics.DrawString($line, $font, [System.Drawing.Brushes]::White, 20, $y); $y+=25 }
        $bitmap.Save("$PSScriptRoot/results.png", [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose(); $font.Dispose() }
    $lines
} finally { $http.Dispose() }
