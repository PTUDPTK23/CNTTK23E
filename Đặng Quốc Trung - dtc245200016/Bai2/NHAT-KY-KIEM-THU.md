# Nhật ký kiểm thử Bài 2

## Thông tin lần chạy

- Thời điểm: 11/11/2026 14:40:43 (UTC+07:00)
- Nhánh: `bai2-dtc245200016`
- HEAD trước khi tạo commit nhật ký: `97b8401`
- Hệ điều hành: Microsoft Windows NT 10.0.26200.0
- PowerShell: 5.1.26100.9711
- .NET SDK: 10.0.102
- Phạm vi: chỉ `Đặng Quốc Trung - dtc245200016/Bai2`

## Lệnh đã chạy

Chạy từ thư mục `Bai2`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\CataLogApi\CatalogApi\tests\Test-Catalog.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File .\RestfulClientServer\tests\Test-Submission.ps1
```

Hai script đều tự build solution, khởi động API tạm thời, gửi request HTTP thật, chạy Client và dừng tiến trình API sau khi hoàn tất.

## Kết quả build

| Solution | Kết quả | Warning | Error |
|---|---|---:|---:|
| `CatalogApi.slnx` | PASS | 0 | 0 |
| `RestfulClientServer.slnx` | PASS | 0 | 0 |

## Đối chiếu 10 tiêu chí Catalog - Bước 10

| # | Tiêu chí | Kết quả quan sát | Trạng thái |
|---:|---|---|---|
| 1 | GET `/` | 200 | PASS |
| 2 | GET `/api/products` | 200, đúng 3 sản phẩm ban đầu | PASS |
| 3 | GET `/api/products/1` | 200 | PASS |
| 4 | GET `/api/products/999` | 404 + `PRODUCT_NOT_FOUND` | PASS |
| 5 | GET `/api/products/abc` | 404, body rỗng | PASS |
| 6 | POST hợp lệ | 201 + `Location` | PASS |
| 7 | POST giá âm | 400 + `INVALID_PRODUCT` | PASS |
| 8 | POST thiếu `Content-Type` | 415 | PASS |
| 9 | Catalog.Client | Danh sách → tạo → chi tiết → đọc lỗi 400 | PASS |
| 10 | Client khi API không kết nối được | In thông báo lỗi mạng có kiểm soát, exit code 0 | PASS |

Tổng kết script:

```text
Catalog integration tests passed: 20 assertions.
```

## Kết quả EduSubmit

| Nhóm ca kiểm thử | Kết quả quan sát | Trạng thái |
|---|---|---|
| Danh sách và chi tiết bài tập | 200 | PASS |
| Bài tập không tồn tại | 404 + `ASSIGNMENT_NOT_FOUND` | PASS |
| ID route sai kiểu | 404, body rỗng | PASS |
| Thiếu `Content-Type` | 415 | PASS |
| JSON hỏng | 400 | PASS |
| Bài tập đã đóng | 400 + `ASSIGNMENT_CLOSED` | PASS |
| Body không hợp lệ | 400 + `INVALID_SUBMISSION` | PASS |
| Tạo lượt nộp | 201 + `Location` | PASS |
| Đọc lại lượt nộp | 200, đúng mã sinh viên | PASS |
| Nộp trùng không phân biệt hoa/thường | 409 + `DUPLICATE_SUBMISSION` | PASS |
| Xóa lượt nộp | 204 | PASS |
| Xóa lại | 404 + `SUBMISSION_NOT_FOUND` | PASS |
| Nộp lại sau khi xóa | 201, ID mới | PASS |
| Submission.Client lần đầu | Danh sách → nộp → đọc lại → lỗi 400 | PASS |
| Submission.Client lần hai | Báo `DUPLICATE_SUBMISSION`, exit code 0 | PASS |
| Client khi API không kết nối được | In thông báo lỗi mạng có kiểm soát, exit code 0 | PASS |

Tổng kết script:

```text
Submission integration tests passed: 35 assertions.
```

## Đối chiếu yêu cầu nộp bài trang 90

| Yêu cầu | Chứng cứ hiện có | Trạng thái |
|---|---|---|
| Đúng thư mục `<Họ tên - MSSV>/Bai2` | `Đặng Quốc Trung - dtc245200016/Bai2` | Đạt |
| API, Client và build không lỗi | Hai solution build 0 warning, 0 error | Đạt |
| `requests.http` tối thiểu 7 request, phủ 200/201/400/404 | Catalog có 8 ca; EduSubmit có 16 ca | Đạt |
| README có lệnh chạy và bảng phá thử | `README.md`, mục 4-7 | Đạt |
| README trả lời ba câu hỏi | `README.md`, mục 9 | Đạt |
| Kết quả kiểm thử có thể chạy lại | Hai script trong thư mục `tests` | Đạt |
| Ảnh kết quả | Chưa chụp; README có danh sách ảnh cần chụp | Chưa hoàn tất |

## Kiểm tra Git và tính trung thực của lịch sử

Trước khi tạo commit cho nhật ký này, lệnh sau trả về `6`:

```powershell
git rev-list --count origin/main..HEAD
```

Nhật ký là phần việc thực tế thứ tư trong đợt bổ sung. Sau khi commit, phải chạy lại lệnh trên và chỉ xác nhận đạt 7 khi Git thực sự trả về `7`.

Không có commit rỗng, không sửa ngày giờ commit, không viết lại lịch sử và không push trong quá trình rà soát. Không có tệp nào ngoài thư mục Bài 2 được sửa.

## Minh chứng còn phải thực hiện thủ công

Ảnh chụp chưa được tạo tự động để tránh minh chứng giả. Trước khi mở Pull Request, cần chụp ít nhất:

1. Hai dòng tổng kết `20 assertions` và `35 assertions`.
2. Build hai solution với 0 warning, 0 error.
3. Phản hồi 201 có `Location`, 400 có `ApiError`, 404 và 415 trong REST Client.
4. Chuỗi EduSubmit 201 → 409 → 204 → 404 → 201.
5. `git status` sạch và `git log --oneline origin/main..HEAD` có đủ commit thật.
