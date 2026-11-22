# Script tự động kiểm thử toàn diện các endpoint HTTP của Auction.API theo chuẩn RESTful
$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "      CHẠY KIỂM THỬ THỰC TẾ CÁC ENDPOINT CỦA AUCTION.API        " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$env:DOTNET_CLI_HOME = "d:\DuAn-DTC245200155-LẦU A CỦA\.dotnet"
$env:APPDATA = "d:\DuAn-DTC245200155-LẦU A CỦA\.dotnet\appdata"
$env:NUGET_PACKAGES = "d:\DuAn-DTC245200155-LẦU A CỦA\.nuget\packages"

$grpcProj = "src/Services/BidValidation.Service/BidValidation.Service.csproj"
$apiProj  = "src/Services/Auction.API/Auction.API.csproj"

# 1. Khởi động gRPC Service (:5002)
Write-Host "1. Khởi động Bid Validation gRPC Service (:5002)..." -ForegroundColor Yellow
$grpcProcess = Start-Process dotnet -ArgumentList "run --project `"$grpcProj`" --no-build" -PassThru -NoNewWindow
Start-Sleep -Seconds 3

# 2. Khởi động Auction API (:5001)
Write-Host "2. Khởi động Auction.API (:5001)..." -ForegroundColor Yellow
$apiProcess = Start-Process dotnet -ArgumentList "run --project `"$apiProj`" --no-build" -PassThru -NoNewWindow

# 3. Chờ API sẵn sàng (Polling Healthcheck)
Write-Host "3. Đang chờ Auction.API khởi động..." -ForegroundColor Yellow
$apiReady = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 600
    try {
        $check = curl.exe -s http://127.0.0.1:5001/health
        if ($check -match "Healthy") {
            $apiReady = $true
            Write-Host "=> Auction.API đã sẵn sàng trên cổng 5001 (Healthcheck: Healthy)!" -ForegroundColor Green
            break
        }
    } catch {}
}

if (-not $apiReady) {
    Write-Host "LỖI: Auction.API không phản hồi sau 18 giây!" -ForegroundColor Red
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $grpcProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

$passed = 0
$failed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [scriptblock]$Action
    )
    Write-Host -NoNewline "[KIỂM TRA] $Name ... "
    try {
        & $Action
        Write-Host "THÀNH CÔNG (PASSED)" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "THẤT BẠI (FAILED): $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

try {
    # Test 1: GET /
    Test-Endpoint "GET / trả về HTTP 200 OK & ServiceInfoDto" {
        $out = curl.exe -s -w "\n%{http_code}" http://127.0.0.1:5001/
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "200") { throw "Status code không phải 200: $code" }
        $json = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($json.service -ne "Auction.API") { throw "Dữ liệu service không đúng: $($json.service)" }
    }

    # Test 2: GET /health
    Test-Endpoint "GET /health trả về HTTP 200 OK 'Healthy'" {
        $out = curl.exe -s -w "\n%{http_code}" http://127.0.0.1:5001/health
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        $content = $lines[0].Trim()
        if ($code -ne "200" -or $content -ne "Healthy") { throw "Healthcheck thất bại: Code=$code, Content=$content" }
    }

    # Test 3: GET /auctions
    Test-Endpoint "GET /auctions trả về HTTP 200 OK & danh sách sản phẩm mẫu" {
        $out = curl.exe -s -w "\n%{http_code}" http://127.0.0.1:5001/auctions
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "200") { throw "Status code: $code" }
        $list = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($list.Count -lt 3) { throw "Số lượng sản phẩm mẫu ít hơn 3: $($list.Count)" }
    }

    # Test 4: GET /auctions/{id} tồn tại
    Test-Endpoint "GET /auctions/{id} tồn tại trả về HTTP 200 OK" {
        $out = curl.exe -s -w "\n%{http_code}" http://127.0.0.1:5001/auctions/11111111-1111-1111-1111-111111111111
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "200") { throw "Status code: $code" }
        $item = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if (-not $item.title.Contains("MacBook")) { throw "Tên sản phẩm không đúng: $($item.title)" }
    }

    # Test 5: GET /auctions/{id} KHÔNG tồn tại -> 404 AUCTION_NOT_FOUND
    Test-Endpoint "GET /auctions/{id} không tồn tại trả về 404 & code AUCTION_NOT_FOUND" {
        $out = curl.exe -s -w "\n%{http_code}" http://127.0.0.1:5001/auctions/99999999-9999-9999-9999-999999999999
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "404") { throw "Status code không phải 404: $code" }
        $body = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($body.code -ne "AUCTION_NOT_FOUND") { throw "Code lỗi không phải AUCTION_NOT_FOUND: $($body.code)" }
    }

    # Test 6: POST /auctions dữ liệu sai -> 400 INVALID_AUCTION_DATA
    Test-Endpoint "POST /auctions dữ liệu sai trả về 400 & code INVALID_AUCTION_DATA" {
        $badJson = '{"title":"","startingPrice":-50000,"stepPrice":0,"startTime":"2026-11-17T00:00:00Z","endTime":"2026-11-16T00:00:00Z"}'
        [System.IO.File]::WriteAllText("temp_bad_auction.json", $badJson)
        $out = curl.exe -s -w "\n%{http_code}" -X POST -H "Content-Type: application/json" -d "@temp_bad_auction.json" http://127.0.0.1:5001/auctions
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "400") { throw "Status code không phải 400: $code" }
        $body = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($body.code -ne "INVALID_AUCTION_DATA") { throw "Code lỗi không phải INVALID_AUCTION_DATA: $($body.code)" }
    }

    # Test 7: POST /auctions hợp lệ -> 201 Created & kiểm tra header Location
    $createdAuctionLocation = ""
    Test-Endpoint "POST /auctions hợp lệ trả về 201 Created & có header Location" {
        $goodJson = '{"title":"Bàn phím cơ Custom Keychron Q1 Pro","description":"Bàn phím nhôm CNC 75% không dây","imageUrl":"https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800","startingPrice":3500000,"stepPrice":100000,"startTime":"2026-11-17T00:00:00Z","endTime":"2026-11-18T23:59:59Z"}'
        [System.IO.File]::WriteAllText("temp_good_auction.json", $goodJson)
        $headerOut = curl.exe -s -i -X POST -H "Content-Type: application/json" -d "@temp_good_auction.json" http://127.0.0.1:5001/auctions
        $raw = $headerOut -join "`n"
        if (-not ($raw -match "HTTP/\S+\s+201")) { throw "Không nhận được HTTP 201 Created! Header output: $raw" }
        if ($raw -match "(?i)location:\s*([^\r\n]+)") {
            $script:createdAuctionLocation = $matches[1].Trim()
        } else {
            throw "Thiếu header Location!"
        }
    }

    # Test 8: POST /auctions/{id}/bids giá thấp -> 400 & code BID_REJECTED (gRPC xác thực)
    Test-Endpoint "POST /auctions/{id}/bids giá thấp trả về 400 & code BID_REJECTED từ gRPC" {
        $lowBidJson = '{"bidder":"NguoiDungTraGiaThap","amount":1000}'
        [System.IO.File]::WriteAllText("temp_low_bid.json", $lowBidJson)
        $out = curl.exe -s -w "\n%{http_code}" -X POST -H "Content-Type: application/json" -d "@temp_low_bid.json" http://127.0.0.1:5001/auctions/11111111-1111-1111-1111-111111111111/bids
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "400") { throw "Status code không phải 400: $code" }
        $body = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($body.code -ne "BID_REJECTED") { throw "Code lỗi không phải BID_REJECTED: $($body.code)" }
    }

    # Test 9: POST /auctions/{id}/bids hợp lệ -> 201 Created & kiểm tra header Location
    $createdBidLocation = ""
    Test-Endpoint "POST /auctions/{id}/bids hợp lệ trả về 201 Created & có header Location" {
        # Đọc giá hiện tại của auction
        $curItem = (curl.exe -s http://127.0.0.1:5001/auctions/11111111-1111-1111-1111-111111111111) | ConvertFrom-Json
        $newPrice = [decimal]($curItem.currentPrice) + [decimal]($curItem.stepPrice) + 1000000

        $validBidJson = "{`"bidder`":`"NguoiDungKiemThu_A`",`"amount`":$newPrice}"
        [System.IO.File]::WriteAllText("temp_valid_bid.json", $validBidJson)

        $headerOut = curl.exe -s -i -X POST -H "Content-Type: application/json" -d "@temp_valid_bid.json" http://127.0.0.1:5001/auctions/11111111-1111-1111-1111-111111111111/bids
        $raw = $headerOut -join "`n"
        if (-not ($raw -match "HTTP/\S+\s+201")) { throw "Không nhận được HTTP 201 Created! Header output: $raw" }
        if ($raw -match "(?i)location:\s*([^\r\n]+)") {
            $script:createdBidLocation = $matches[1].Trim()
        } else {
            throw "Thiếu header Location!"
        }
    }

    # Test 10: GET đọc lại lượt đặt giá từ header Location -> 200 OK
    Test-Endpoint "GET đọc lại lượt đặt giá từ header Location trả về HTTP 200 OK" {
        if ([string]::IsNullOrWhiteSpace($script:createdBidLocation)) { throw "Không có Location từ test trước" }
        $out = curl.exe -s -w "\n%{http_code}" "http://127.0.0.1:5001$($script:createdBidLocation)"
        $lines = $out -split "`n"
        $code = $lines[-1].Trim()
        if ($code -ne "200") { throw "Status code không phải 200: $code" }
        $bid = ($lines[0..($lines.Count - 2)] -join "`n") | ConvertFrom-Json
        if ($bid.bidder -ne "NguoiDungKiemThu_A") { throw "Dữ liệu bidder không khớp: $($bid.bidder)" }
    }

    # Test 11: DELETE /auctions/{id} -> 204 No Content
    Test-Endpoint "DELETE /auctions/{id} trả về HTTP 204 No Content" {
        if (-not [string]::IsNullOrWhiteSpace($script:createdAuctionLocation)) {
            $headerOut = curl.exe -s -i -X DELETE "http://127.0.0.1:5001$($script:createdAuctionLocation)"
            $raw = $headerOut -join "`n"
            if (-not ($raw -match "HTTP/\S+\s+204")) { throw "Mã trả về không phải 204! Header: $raw" }
        }
    }
}
finally {
    # Dọn dẹp file tạm
    Remove-Item -Force -ErrorAction SilentlyContinue temp_bad_auction.json, temp_good_auction.json, temp_low_bid.json, temp_valid_bid.json

    Write-Host "`nDọn dẹp và dừng các tiến trình server kiểm thử..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $grpcProcess.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "KẾT QUẢ KIỂM THỬ: $passed PASSED, $failed FAILED" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "=================================================================" -ForegroundColor Cyan
