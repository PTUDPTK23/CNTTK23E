# Bài 2 – Catalog RESTful API

**Sinh viên:** Lưu Quang Lực – dtc245200349

## Chạy bài

Chạy các lệnh từ thư mục này. Dùng .NET 10 SDK.

```powershell
dotnet build .\CatalogApi.slnx
dotnet run --project .\src\Catalog.Api --urls http://localhost:5100
```

Giữ API chạy và mở terminal thứ hai.

```powershell
dotnet run --project .\src\Catalog.Client -- http://localhost:5100
```

## Kết quả kiểm tra

- Build thành công với 0 cảnh báo và 0 lỗi.
- Tám yêu cầu trong `requests.http` trả lần lượt `200`, `200`, `200`, `404`, `404`, `201`, `400`, `415`.
- Lần chạy đầu sau khi khởi động lại API, client đọc 3 sản phẩm, tạo sản phẩm ID 4, nhận `Location: /api/products/4`, đọc chi tiết sản phẩm ID 1 và nhận `400 INVALID_PRODUCT` khi gửi giá âm và tên rỗng.

| Thí nghiệm | Kết quả quan sát |
| --- | --- |
| Chạy client lần hai, giữ nguyên API | Client đọc 4 sản phẩm và tạo sản phẩm ID 5. |
| Tắt API rồi chạy client ở cổng 5100 | Client báo lỗi kết nối tới `localhost:5100`. |
| Khởi động lại API rồi gọi `GET /api/products` | Danh sách trở về 3 sản phẩm mẫu. |
| Giữ API ở cổng 5100, cho client gọi cổng 5999 | Client báo lỗi kết nối tới `localhost:5999`. |

## Câu hỏi

1. **Vì sao URI dùng danh từ?** URI xác định tài nguyên, chẳng hạn `/api/products`. Phương thức HTTP xác định thao tác trên tài nguyên đó. `GET` đọc danh sách và `POST` tạo sản phẩm.
2. **Stateless thể hiện ở đâu?** Mỗi yêu cầu mang đủ thông tin để API xử lý. Yêu cầu lấy chi tiết có ID trong URI, còn yêu cầu tạo có dữ liệu trong JSON. API lưu danh sách sản phẩm, nhưng không cần ghi nhớ phiên của client để xử lý yêu cầu tiếp theo.
3. **Vì sao DTO khai báo riêng vẫn khớp?** Hai chương trình trao đổi JSON theo cùng hợp đồng tên và kiểu dữ liệu. Mỗi bên có thể khai báo `ProductDto` riêng và ánh xạ các trường JSON `id`, `name`, `description` và `price`.
