# MediQueue UI Design System

## 1. Định hướng

Giao diện Prompt 03.3.1 theo phong cách Modern Healthcare SaaS: tin cậy, sáng, nhiều khoảng thở, tương phản rõ và không dùng dữ liệu/hình ảnh bệnh nhân thật. UI hỗ trợ bảo mật và khả dụng; không thay thế authorization tại API.

## 2. Design token

| Nhóm | Token chính |
|---|---|
| Brand | Primary `#2563EB`, Secondary `#0D9488` |
| Nền | Background `#F8FAFC`, Surface `#FFFFFF`, Surface alt `#F1F5F9` |
| Chữ | Text `#0F172A`, Secondary `#475569`, Muted `#64748B` |
| Trạng thái | Success `#16A34A`, Warning `#F59E0B`, Error `#DC2626` |
| Bo góc | `10px`, `16px`, `24px`, `32px` theo cấp component |
| Khoảng cách | Thang 4/8/12/16/20/24/32/40/48/64/80px |
| Motion | 160ms nhanh, 260ms chuẩn, easing `cubic-bezier(.2,.8,.2,1)` |

Token nằm ở `wwwroot/css/tokens.css`; component chung ở `components.css`, trang công khai ở `site.css`, auth ở `auth.css`, account ở `account.css`.

## 3. Typography và giấy phép

- Font: **Be Vietnam Pro** weight 400, 500, 600, 700, lưu local dưới `wwwroot/fonts`.
- Nguồn: repository `google/fonts/ofl/bevietnampro` chính thức; copyright 2021 The Be Vietnam Pro Project Authors.
- License: SIL Open Font License 1.1, bản sao tại `wwwroot/fonts/OFL.txt`.
- `font-display: swap`; fallback `Inter`, `Segoe UI`, sans-serif. Body tối thiểu 16px; label 14px.

Không gọi Google Fonts CDN khi runtime. Phiên bản nguồn chính thức cung cấp TTF; bốn weight cần thiết được chọn thay vì toàn bộ family để giới hạn tải.

## 4. Component và interaction

- Brand/logo, header responsive, user dropdown và footer dùng chung.
- Button có hover elevation, active press, focus-visible và loading spinner chống submit lặp.
- Input có icon, focus ring, server/client validation và password toggle có `aria-pressed`.
- Password checklist phản hồi độ dài/tính đa dạng; backend vẫn là nguồn kiểm tra cuối.
- Toast success/warning/error có live region và nút đóng; tự ẩn sau 6,5 giây.
- Status page cho access denied và lỗi dịch vụ; không lộ exception/secret.
- Icon dùng SVG sprite local với stroke nhất quán; minh họa SVG là asset tự xây dựng.

## 5. Responsive và accessibility

- Thiết kế kiểm tra từ 320px đến 1920px; auth split layout chuyển thành single panel dưới 820px.
- Account layout chuyển từ sidebar card sang stacked navigation dưới 900px; form hai cột chuyển một cột dưới 600px.
- Có skip link, semantic heading/landmark, label thật, `aria-live`, keyboard focus và mục tiêu tương tác tối thiểu khoảng 44px.
- Màu chữ/nền ưu tiên WCAG AA; không dùng màu là tín hiệu duy nhất.
- `prefers-reduced-motion: reduce` vô hiệu hóa animation/transition không thiết yếu.

## 6. Performance và giới hạn

- Asset/font/icon đều local; không thêm UI/icon package hoặc CDN.
- SVG có kích thước nội tại để hạn chế layout shift; CSS chia theo phạm vi và JavaScript thuần, không xử lý token.
- Prompt 03.3.1 không tạo dashboard/sidebar/empty CRUD giả. Các component đó chỉ được bổ sung cùng dữ liệu và authorization thật ở prompt sau.
