# CNTT K23E

**Trường Đại học Công nghệ Thông tin & Truyền thông - Đại học Thái Nguyên**

Thư mục nộp bài tập và đồ án của sinh viên lớp **Phát triển ứng dụng phân tán** (K23.K1.D2.2627.CNTT K23E).

## 📢 Thông báo – Hạn sửa Bài 1, Bài 2 (cập nhật 17/11/2026)

1. Chỉ chấm những bạn **đã có Pull Request (PR)**. Bạn nào chưa nộp PR thì không chấm.
2. PR đang bị "Changes requested" hoặc có nhận xét lỗi: đọc lý do cô ghi ở tab **Conversation**, sửa trong fork của em rồi **commit + push thêm vào đúng nhánh của PR đó**. **Không mở PR mới, không đóng PR cũ** (PR mới nộp sau hạn sẽ bị hệ thống tự đóng).
3. Dòng "Quá hạn" do hệ thống tự gắn chỉ để ghi nhận. Bài sửa đến hạn bên dưới vẫn được chấm, không trừ điểm nộp muộn.
4. **Hạn cuối để push bản sửa: 17h00 ngày 18/11/2026.** Sau giờ này cô không xem thêm.
5. Trước khi push, tự kiểm: bài nằm trong `<Họ tên> - <MSSV>/Bai2/`; không có `bin/`, `obj/` (có `.gitignore`); `requests.http` đủ 7 request có 200, 201, 400, 404; README có lệnh chạy, ảnh/kết quả chạy, bảng phá thử, trả lời 3 câu hỏi.
6. Sau khi push, bình luận 1 dòng trong PR để cô biết đã sửa.

Tối 18/11 (sau 17h00) cô duyệt và chấm lần cuối cho Bài 1, Bài 2, sau đó chuyển sang Bài 3. Bài 3 sẽ có hạn nộp cố định, rõ ràng, không nhắc và không chấm lại.


## Hướng dẫn nộp bài (Fork + Pull Request)
> Sinh viên chỉ có quyền **xem** repo này, **không push trực tiếp** được. Mọi bài nộp đều gửi qua **Pull Request (PR)**; giảng viên kiểm tra rồi mới gộp vào.

1. **Fork repo:** bấm nút **Fork** (góc trên bên phải) → **Create fork**. Bạn sẽ có bản sao riêng: `<tài-khoản-github>/CNTTK23E`.
2. **Thêm bài vào fork của mình:** trong thư mục `[Họ và tên] - [Mã sinh viên]` của mình, tạo thư mục bài (`Bai1/`, `BaiTap1/`, `BTL/`, ...) rồi đưa bài vào.
   - Trên web: vào thư mục của mình → **Add file → Upload files** → **Commit changes**.
   - Hoặc dùng git:
     ```bash
     git clone https://github.com/<tài-khoản-github>/CNTTK23E.git
     cd CNTTK23E
     # chép bài vào thư mục "<Họ và tên> - <Mã sinh viên>/Bai1/"
     git add .
     git commit -m "Bai1 - <Mã sinh viên> - <Họ và tên>"
     git push
     ```
3. **Tạo Pull Request:** ở trang fork, bấm **Contribute → Open pull request**. Tiêu đề PR **bắt buộc** theo mẫu: `Bai1 - dtc2452xxxxx - Nguyễn Văn A` (bài 2 thì `Bai2 - ...`). Bấm **Create pull request** là xong.
4. **Sửa / bổ sung bài:** không tạo PR mới, chỉ cần commit thêm vào fork, PR cũ tự cập nhật. Trước khi làm bài mới, bấm **Sync fork** để fork có nội dung mới nhất.

### Ví dụ: tạo thư mục bài và dự án

Giả sử sinh viên **Nguyễn Văn A – dtc245200xxx**. Trong thư mục của mình, mỗi bài/dự án là một thư mục con:

```
Nguyễn Văn A - dtc245200xxx/
├── Bai1/
│   ├── README.md
│   └── (mã nguồn, báo cáo ...)
├── Bai2/
│   └── ...
└── DuAn/
    └── ...
```

Tạo thư mục và nộp bằng git:

```bash
cd "Nguyễn Văn A - dtc245200xxx"
mkdir Bai2          # dự án thì: mkdir DuAn
# chép bài vào Bai2/
git add .
git commit -m "Bai2 - dtc245200xxx - Nguyễn Văn A"
git push
```

Sau đó mở Pull Request với tiêu đề `Bai2 - dtc245200xxx - Nguyễn Văn A`.

### Cập nhật fork bằng git pull (trước khi làm bài mới)

Chỉ làm một lần, để máy biết repo gốc của lớp:

```bash
git remote add upstream https://github.com/PTUDPTK23/CNTTK23E.git
```

Mỗi lần trước khi làm bài mới:

```bash
git checkout main
git pull upstream main    # lấy nội dung mới nhất từ repo của lớp
git push origin main      # đẩy bản đã cập nhật lên fork của bạn
```

Nếu làm trên web thì bấm **Sync fork → Update branch** ở trang fork.

### Quy định
- Chỉ thêm/sửa file **trong thư mục của chính mình**. PR sửa thư mục của bạn khác hoặc file chung (README, .github, ...) sẽ bị từ chối.
- Hệ thống tự gắn nhãn theo thời điểm mở/cập nhật PR: **Đúng hạn**, **Muộn**, **Quá hạn**.
- **Bài 2:** đúng hạn trước **12:00 ngày 11/11/2026**; nộp muộn đến **23:59 ngày 11/11/2026**; sau thời điểm này **đóng link nộp**: PR mở mới sẽ bị đóng tự động.

## Danh sách sinh viên

| STT | Mã sinh viên | Họ và tên | Thư mục nộp bài |
|:---:|--------------|-----------|-----------------|
| 1 | dtc245200156 | Giàng Hồng Anh | `Giàng Hồng Anh - dtc245200156` |
| 2 | dtc235200025 | Hoàng Tiến Anh | `Hoàng Tiến Anh - dtc235200025` |
| 3 | dtc245200107 | Hoàng Thị Kiều Anh | `Hoàng Thị Kiều Anh - dtc245200107` |
| 4 | dtc245200851 | Lê Hải Anh | `Lê Hải Anh - dtc245200851` |
| 5 | dtc245200615 | Trần Tuấn Anh | `Trần Tuấn Anh - dtc245200615` |
| 6 | dtc245200439 | Lò Văn Bắc | `Lò Văn Bắc - dtc245200439` |
| 7 | dtc245200302 | Thùng Tuấn Bằng | `Thùng Tuấn Bằng - dtc245200302` |
| 8 | dtc245200711 | Thào A Chả | `Thào A Chả - dtc245200711` |
| 9 | dtc245200559 | Hoàng Công Chiến | `Hoàng Công Chiến - dtc245200559` |
| 10 | dtc245200155 | Lầu A Của | `Lầu A Của - dtc245200155` |
| 11 | dtc245200041 | Sìn Văn Cương | `Sìn Văn Cương - dtc245200041` |
| 12 | dtc245200607 | Sùng A Dà | `Sùng A Dà - dtc245200607` |
| 13 | dtc245200124 | Nguyễn Mạnh Cường | `Nguyễn Mạnh Cường - dtc245200124` |
| 14 | dtc245200226 | Hoàng Xuân Đoàn | `Hoàng Xuân Đoàn - dtc245200226` |
| 15 | dtc245200209 | Trần Thị Dung | `Trần Thị Dung - dtc245200209` |
| 16 | dtc245200623 | Hoàng Lý Trần Dương | `Hoàng Lý Trần Dương - dtc245200623` |
| 17 | dtc245200859 | Phạm Văn Dương | `Phạm Văn Dương - dtc245200859` |
| 18 | dtc245200361 | Vàng Văn Duy | `Vàng Văn Duy - dtc245200361` |
| 19 | dtc245200988 | Lường Thị Giang | `Lường Thị Giang - dtc245200988` |
| 20 | dtc241200006 | Lý Thị Hà | `Lý Thị Hà - dtc241200006` |
| 21 | dtc245200767 | Lý Huy Hiệu | `Lý Huy Hiệu - dtc245200767` |
| 22 | dtc245200849 | Nguyễn Hoàng Giang | `Nguyễn Hoàng Giang - dtc245200849` |
| 23 | dtc245200039 | Bùi Thị Ngọc Hoa | `Bùi Thị Ngọc Hoa - dtc245200039` |
| 24 | dtc245200476 | Nguyễn Xuân Hợp | `Nguyễn Xuân Hợp - dtc245200476` |
| 25 | dtc245200125 | Lô Quốc Khánh | `Lô Quốc Khánh - dtc245200125` |
| 26 | dtc245200665 | Giáp Đăng Khoa | `Giáp Đăng Khoa - dtc245200665` |
| 27 | dtc245200131 | Vương Quốc Khoa | `Vương Quốc Khoa - dtc245200131` |
| 28 | dtc245200287 | Phạm Bảo Lâm | `Phạm Bảo Lâm - dtc245200287` |
| 29 | dtc245200680 | Hoàng Đức Lâm | `Hoàng Đức Lâm - dtc245200680` |
| 30 | dtc245200571 | Thàng Xuân Lập | `Thàng Xuân Lập - dtc245200571` |
| 31 | dtc245200363 | Giàng A Lênh | `Giàng A Lênh - dtc245200363` |
| 32 | dtc245200858 | Dương Thị Khánh Linh | `Dương Thị Khánh Linh - dtc245200858` |
| 33 | dtc245200327 | Nguyễn Doãn Thành Long | `Nguyễn Doãn Thành Long - dtc245200327` |
| 34 | dtc245200349 | Lưu Quang Lực | `Lưu Quang Lực - dtc245200349` |
| 35 | dtc245200198 | Nguyễn Khánh Ly | `Nguyễn Khánh Ly - dtc245200198` |
| 36 | dtc245200002 | Hà Đức Minh | `Hà Đức Minh - dtc245200002` |
| 37 | dtc245200413 | Nguyễn Thị Hồng Nhung | `Nguyễn Thị Hồng Nhung - dtc245200413` |
| 38 | dtc245200480 | Nguyễn Xuân Phú | `Nguyễn Xuân Phú - dtc245200480` |
| 39 | dtc245200592 | Thào A Pông | `Thào A Pông - dtc245200592` |
| 40 | dtc245200935 | Mã Dương Quốc | `Mã Dương Quốc - dtc245200935` |
| 41 | dtc241200007 | Vàng Thị Sinh | `Vàng Thị Sinh - dtc241200007` |
| 42 | dtc245200852 | Nguyễn Anh Sơn | `Nguyễn Anh Sơn - dtc245200852` |
| 43 | dtc245200344 | Long Minh Thành | `Long Minh Thành - dtc245200344` |
| 44 | dtc245200501 | Dương Văn Thịnh | `Dương Văn Thịnh - dtc245200501` |
| 45 | dtc245200184 | Nguyễn Tiến Thịnh | `Nguyễn Tiến Thịnh - dtc245200184` |
| 46 | dtc245200850 | Đường Thị Tiến | `Đường Thị Tiến - dtc245200850` |
| 47 | dtc245200860 | Trần Thị Huyền Trang | `Trần Thị Huyền Trang - dtc245200860` |
| 48 | dtc245200016 | Đặng Quốc Trung | `Đặng Quốc Trung - dtc245200016` |
| 49 | dtc245200120 | Nguyễn Duy Trường | `Nguyễn Duy Trường - dtc245200120` |
| 50 | dtc245200019 | Diệp Quang Trường | `Diệp Quang Trường - dtc245200019` |
| 51 | dtc245200758 | Hoàng Văn Tuấn | `Hoàng Văn Tuấn - dtc245200758` |
| 52 | dtc245200250 | Phan Quang Tùng | `Phan Quang Tùng - dtc245200250` |
| 53 | dtc245200211 | Trịnh Ngọc Tường | `Trịnh Ngọc Tường - dtc245200211` |
| 54 | dtc245200078 | Bế Thị Thu Uyên | `Bế Thị Thu Uyên - dtc245200078` |
| 55 | dtc245200700 | Hoàng Thế Văn | `Hoàng Thế Văn - dtc245200700` |
| 56 | dtc245200682 | Đàm Văn Việt | `Đàm Văn Việt - dtc245200682` |
| 57 | dtc245200309 | Phan Thế Vinh | `Phan Thế Vinh - dtc245200309` |
| 58 | dtc245200194 | Lý A Vinh | `Lý A Vinh - dtc245200194` |
| 59 | dtc245200014 | Giàng Minh Vũ | `Giàng Minh Vũ - dtc245200014` |
| 60 | dtc241200004 | Phạm Long Vũ | `Phạm Long Vũ - dtc241200004` |
| 61 | dtc251200016 | Nguyễn Công Vĩnh | `Nguyễn Công Vĩnh - dtc251200016` |

## Hướng dẫn nộp dự án – Phát triển ứng dụng phân tán (CNTT K23E)

### 1. Quy định chung
- Dự án kéo dài **9 tuần**. Mỗi tuần nộp vào một thư mục riêng: `Tuan1`, `Tuan2`, … `Tuan9`.
- Mỗi sinh viên làm **đúng đề tài đã đăng ký**. Tên thư mục dự án ghi trong bảng ở mục 4, **không tự đặt hoặc đổi tên**.
- **Hạn nộp Tuần 1 của lớp K23E: 23h59 thứ Ba 17/11/2026.** Sau hạn này Tuần 1 không nhận bài. Hạn các tuần sau sẽ được thông báo.

### 2. Cấu trúc thư mục
Trong thư mục cá nhân của mình, dự án nằm trong `DuAn/`, bên trong là thư mục tên dự án (theo bảng mục 4), rồi đến từng tuần:

```
Nguyễn Văn A - dtc245200xxx/
└── DuAn/
    └── DT03_HeThongDatVeSuKienPhanTan/     <- tên dự án đúng theo bảng mục 4
        ├── Tuan1/
        │   ├── README.md
        │   └── (mã nguồn, báo cáo ...)
        └── Tuan2/ ...
```

### 3. Cách nộp
1. Tạo thư mục theo đúng đường dẫn ở bảng mục 4, đặt toàn bộ bài làm Tuần 1 vào `Tuan1/`.
2. Commit, push và mở **Pull Request** vào nhánh `main` của repo này (giống cách nộp Bài 1, Bài 2).
3. Đặt tên PR: `DuAn-Tuan1 - MSSV - Họ tên`
4. Mỗi tuần chỉ **một PR**. Giảng viên yêu cầu sửa thì **push thêm commit vào chính PR đó**, không mở PR mới. Việc sửa cũng phải xong trước hạn.
5. Chỉ thêm file trong thư mục của mình. PR sửa thư mục của bạn khác hoặc sai đường dẫn sẽ bị trả lại.

### 4. Tên dự án và thư mục nộp Tuần 1 của từng sinh viên
Sinh viên không có tên trong bảng (chưa đăng ký đề tài) liên hệ giảng viên trước hạn nộp.

| STT | MSSV | Họ và tên | Đề tài | Thư mục nộp Tuần 1 |
|---|---|---|---|---|
| 1 | dtc245200156 | Giàng Hồng Anh | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Giàng Hồng Anh - dtc245200156/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 2 | dtc235200025 | Hoàng Tiến Anh | 10 – Hệ thống đấu giá trực tuyến | `Hoàng Tiến Anh - dtc235200025/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 3 | dtc245200107 | Hoàng Thị Kiều Anh | 08 – Hệ thống theo dõi bưu gửi | `Hoàng Thị Kiều Anh - dtc245200107/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 4 | dtc245200851 | Lê Hải Anh | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Lê Hải Anh - dtc245200851/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 5 | dtc245200615 | Trần Tuấn Anh | 10 – Hệ thống đấu giá trực tuyến | `Trần Tuấn Anh - dtc245200615/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 6 | dtc245200711 | Thào A Chả | 06 – Hệ thống giám sát cảm biến IoT | `Thào A Chả - dtc245200711/DuAn/DT06_HeThongGiamSatCamBienIot/Tuan1/` |
| 7 | dtc245200041 | Sìn Văn Cương | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Sìn Văn Cương - dtc245200041/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 8 | dtc245200607 | Sùng A Dà | 07 – Hệ thống hỗ trợ yêu cầu khách hàng | `Sùng A Dà - dtc245200607/DuAn/DT07_HeThongHoTroYeuCauKhachHang/Tuan1/` |
| 9 | dtc245200226 | Hoàng Xuân Đoàn | 04 – Hệ thống đặt món và xử lý tại bếp | `Hoàng Xuân Đoàn - dtc245200226/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 10 | dtc245200209 | Trần Thị Dung | 11 – Hệ thống gửi thông báo hàng loạt | `Trần Thị Dung - dtc245200209/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 11 | dtc245200623 | Hoàng Lý Trần Dương | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Hoàng Lý Trần Dương - dtc245200623/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 12 | dtc245200859 | Phạm Văn Dương | 02 – Hệ thống đăng ký học phần thời gian thực | `Phạm Văn Dương - dtc245200859/DuAn/DT02_HeThongDangKyHocPhanThoiGianThuc/Tuan1/` |
| 13 | dtc245200988 | Lường Thị Giang | 12 – Hệ thống phê duyệt tài liệu phân tán | `Lường Thị Giang - dtc245200988/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 14 | dtc241200006 | Lý Thị Hà | 12 – Hệ thống phê duyệt tài liệu phân tán | `Lý Thị Hà - dtc241200006/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 15 | dtc245200767 | Lý Huy Hiệu | 05 – Hệ thống xử lý tệp bất đồng bộ | `Lý Huy Hiệu - dtc245200767/DuAn/DT05_HeThongXuLyTepBatDongBo/Tuan1/` |
| 16 | dtc245200039 | Bùi Thị Ngọc Hoa | 11 – Hệ thống gửi thông báo hàng loạt | `Bùi Thị Ngọc Hoa - dtc245200039/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 17 | dtc245200476 | Nguyễn Xuân Hợp | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Nguyễn Xuân Hợp - dtc245200476/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 18 | dtc245200125 | Lô Quốc Khánh | 18 – Hệ thống đăng ký sự kiện và check-in | `Lô Quốc Khánh - dtc245200125/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 19 | dtc245200665 | Giáp Đăng Khoa | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Giáp Đăng Khoa - dtc245200665/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 20 | dtc245200131 | Vương Quốc Khoa | 04 – Hệ thống đặt món và xử lý tại bếp | `Vương Quốc Khoa - dtc245200131/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 21 | dtc245200287 | Phạm Bảo Lâm | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Phạm Bảo Lâm - dtc245200287/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 22 | dtc245200680 | Hoàng Đức Lâm | 11 – Hệ thống gửi thông báo hàng loạt | `Hoàng Đức Lâm - dtc245200680/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 23 | dtc245200571 | Thàng Xuân Lập | 15 – Hệ thống nộp bài và chấm bài tự động | `Thàng Xuân Lập - dtc245200571/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 24 | dtc245200363 | Giàng A Lênh | 16 – Hệ thống chat hỗ trợ trực tuyến | `Giàng A Lênh - dtc245200363/DuAn/DT16_HeThongChatHoTroTrucTuyen/Tuan1/` |
| 25 | dtc245200858 | Dương Thị Khánh Linh | 08 – Hệ thống theo dõi bưu gửi | `Dương Thị Khánh Linh - dtc245200858/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 26 | dtc245200327 | Nguyễn Doãn Thành Long | 15 – Hệ thống nộp bài và chấm bài tự động | `Nguyễn Doãn Thành Long - dtc245200327/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 27 | dtc245200349 | Lưu Quang Lực | 11 – Hệ thống gửi thông báo hàng loạt | `Lưu Quang Lực - dtc245200349/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 28 | dtc245200198 | Nguyễn Khánh Ly | 03 – Hệ thống đặt vé sự kiện phân tán | `Nguyễn Khánh Ly - dtc245200198/DuAn/DT03_HeThongDatVeSuKienPhanTan/Tuan1/` |
| 29 | dtc245200002 | Hà Đức Minh | 10 – Hệ thống đấu giá trực tuyến | `Hà Đức Minh - dtc245200002/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 30 | dtc245200413 | Nguyễn Thị Hồng Nhung | 12 – Hệ thống phê duyệt tài liệu phân tán | `Nguyễn Thị Hồng Nhung - dtc245200413/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 31 | dtc245200592 | Thào A Pông | 17 – Hệ thống đặt chuyến xe | `Thào A Pông - dtc245200592/DuAn/DT17_HeThongDatChuyenXe/Tuan1/` |
| 32 | dtc245200935 | Mã Dương Quốc | 11 – Hệ thống gửi thông báo hàng loạt | `Mã Dương Quốc - dtc245200935/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 33 | dtc241200007 | Vàng Thị Sinh | 12 – Hệ thống phê duyệt tài liệu phân tán | `Vàng Thị Sinh - dtc241200007/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 34 | dtc245200852 | Nguyễn Anh Sơn | 08 – Hệ thống theo dõi bưu gửi | `Nguyễn Anh Sơn - dtc245200852/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 35 | dtc245200344 | Long Minh Thành | 10 – Hệ thống đấu giá trực tuyến | `Long Minh Thành - dtc245200344/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 36 | dtc245200501 | Dương Văn Thịnh | 10 – Hệ thống đấu giá trực tuyến | `Dương Văn Thịnh - dtc245200501/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 37 | dtc245200184 | Nguyễn Tiến Thịnh | 04 – Hệ thống đặt món và xử lý tại bếp | `Nguyễn Tiến Thịnh - dtc245200184/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 38 | dtc245200850 | Đường Thị Tiến | 11 – Hệ thống gửi thông báo hàng loạt | `Đường Thị Tiến - dtc245200850/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 39 | dtc245200016 | Đặng Quốc Trung | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Đặng Quốc Trung - dtc245200016/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 40 | dtc245200120 | Nguyễn Duy Trường | 08 – Hệ thống theo dõi bưu gửi | `Nguyễn Duy Trường - dtc245200120/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 41 | dtc245200019 | Diệp Quang Trường | 10 – Hệ thống đấu giá trực tuyến | `Diệp Quang Trường - dtc245200019/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 42 | dtc245200758 | Hoàng Văn Tuấn | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Hoàng Văn Tuấn - dtc245200758/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 43 | dtc245200250 | Phan Quang Tùng | 18 – Hệ thống đăng ký sự kiện và check-in | `Phan Quang Tùng - dtc245200250/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 44 | dtc245200211 | Trịnh Ngọc Tường | 15 – Hệ thống nộp bài và chấm bài tự động | `Trịnh Ngọc Tường - dtc245200211/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 45 | dtc245200078 | Bế Thị Thu Uyên | 12 – Hệ thống phê duyệt tài liệu phân tán | `Bế Thị Thu Uyên - dtc245200078/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 46 | dtc245200682 | Đàm Văn Việt | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Đàm Văn Việt - dtc245200682/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 47 | dtc245200309 | Phan Thế Vinh | 05 – Hệ thống xử lý tệp bất đồng bộ | `Phan Thế Vinh - dtc245200309/DuAn/DT05_HeThongXuLyTepBatDongBo/Tuan1/` |
| 48 | dtc245200194 | Lý A Vinh | 01 – Hệ thống xử lý đơn hàng và thông báo trạng thái phân tán | `Lý A Vinh - dtc245200194/DuAn/DT01_HeThongXuLyDonHangVaThongBaoTrangThaiPhanTan/Tuan1/` |
| 49 | dtc245200014 | Giàng Minh Vũ | 18 – Hệ thống đăng ký sự kiện và check-in | `Giàng Minh Vũ - dtc245200014/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 50 | dtc241200004 | Phạm Long Vũ | 19 – Hệ thống tra cứu dữ liệu học tập bằng GraphQL | `Phạm Long Vũ - dtc241200004/DuAn/DT19_HeThongTraCuuDuLieuHocTapBangGraphQL/Tuan1/` |
| 51 | dtc245200849 | Nguyễn Hoàng Giang | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Nguyễn Hoàng Giang - dtc245200849/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 52 | dtc245200124 | Nguyễn Mạnh Cường | 01 – Hệ thống xử lý đơn hàng và thông báo trạng thái phân tán | `Nguyễn Mạnh Cường - dtc245200124/DuAn/DT01_HeThongXuLyDonHangVaThongBaoTrangThaiPhanTan/Tuan1/` |
