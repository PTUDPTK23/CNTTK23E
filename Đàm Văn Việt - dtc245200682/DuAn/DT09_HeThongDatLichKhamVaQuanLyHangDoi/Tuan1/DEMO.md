# Kịch bản demo đề tài 09

## Chuẩn bị

1. Mở Docker Desktop, chạy `docker compose up -d --build` từ DuAn.
2. Kiểm tra `docker compose ps`, truy cập `http://localhost:5080/health`.
3. Mở hai cửa sổ: bệnh nhân `http://localhost:5080/`, nhân viên `http://localhost:5080/staff.html`.
4. Đăng nhập nhân viên bằng mật khẩu trong cấu hình demo; không chiếu `.env` lên màn hình.
5. Chọn cùng bác sĩ, ngày hôm nay và khung đang diễn ra. Đảm bảo còn chỗ; không xóa volume để lấy chỗ trống.

## Trình bày luồng bình thường (khoảng 3 phút)

1. Bệnh nhân chọn bác sĩ/ngày, xem chỗ còn lại rồi đặt lịch. Lưu cả mã và khóa.
2. Giải thích API giữ chỗ, tạo Pending và outbox trong một transaction. RabbitMQ chuyển yêu cầu đến Worker; Worker mới cấp số.
3. Nhân viên gọi lượt tiếp theo; cửa sổ bệnh nhân tự đổi lượt đang phục vụ và trạng thái Serving qua SignalR.
4. Nhân viên hoàn thành; bệnh nhân thấy Completed. Reload và kiểm tra bác sĩ/ngày vẫn khớp lịch đã lưu.
5. Nêu rõ Notification nhận sự kiện qua RabbitMQ, gửi tín hiệu để web lấy snapshot mới từ API.

## Minh họa phục hồi (khoảng 3 phút)

1. `docker compose stop worker`: đặt một lịch mới. Lịch ở Pending; cửa sổ bệnh nhân khác vẫn thấy số chỗ giảm.
2. `docker compose start worker`: lịch được cấp số mà không cần gửi lại yêu cầu đặt.
3. Cho tab bệnh nhân offline bằng DevTools, thao tác ở nhân viên, bật lại mạng: SignalR tham gia lại nhóm và tải trạng thái hiện tại.
4. Nếu cần minh họa broker, `docker compose stop rabbitmq`, đặt lịch rồi `docker compose start rabbitmq`: outbox giữ yêu cầu và tự phát lại. Chờ kết nối phục hồi.

## Minh họa chống trùng bằng bộ kiểm thử

`dotnet run --project tests/Clinic.Smoke --no-build` chạy cả các tình huống dừng/bật container. Chỉ chạy khi không có người đang dùng demo. Bộ test tạo fixture riêng và dọn fixture đó.

Ca HTTP gửi tám POST cùng khóa đồng thời, kiểm tra chỉ một lịch/một chỗ/một yêu cầu cấp số, sau đó restart API và retry. Ca trình duyệt chặn phản hồi sau khi server đã xử lý, reload rồi bấm thử lại: vẫn chỉ một lịch. Không diễn giải idempotency là cấm bệnh nhân chủ động đặt nhiều lịch khác nhau.

## Câu hỏi cần giải thích

- Vì sao dùng RabbitMQ: tách cấp số khỏi HTTP và tiếp nhận yêu cầu khi Worker tạm dừng.
- Vì sao outbox: tránh DB đã lưu nhưng sự kiện bị mất khi broker lỗi.
- Vì sao message có thể trùng: crash sau publish trước khi đánh dấu sent; consumer phải idempotent.
- Vì sao không vượt chỗ: cập nhật có điều kiện và ràng buộc capacity trong transaction.
- SignalR khác RabbitMQ: SignalR cập nhật trình duyệt; RabbitMQ truyền công việc/sự kiện giữa service.
- Giới hạn: lịch bác sĩ seed sẵn, một Notification instance, chưa có OTP/SMS hoặc đo tải lớn.

Sau demo để các service hoạt động lại bằng `docker compose start rabbitmq worker`. Không dùng `docker compose down -v` vì sẽ xóa dữ liệu.
