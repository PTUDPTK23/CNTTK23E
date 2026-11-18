# Chỉ mục 15 ảnh minh chứng Bài 3

Các ảnh do sinh viên chụp trực tiếp khi chạy chương trình trong Windows Terminal. Giữ nguyên tệp PNG gốc; bấm tên tệp để mở ảnh đầy đủ.

| STT | Mốc | Ảnh gốc | Kết quả nhìn thấy |
|---|---|---|---|
| 1 | Bước 1 — build Ping | [buoc1-build.png](buoc1-build.png) | Hai project Ping, Build succeeded, 0 Error(s). |
| 2 | Bước 2 — negotiate | [buoc2-negotiate.png](buoc2-negotiate.png) | curl POST, 200 OK, connectionId và transport. |
| 3 | Bước 3–4 — Echo | [buoc4-echo.png](buoc4-echo.png) | Máy chủ lắng nghe; An nhận Echo trong cùng cửa sổ. |
| 4 | Bước 5 — nhóm | [buoc5-nhom.png](buoc5-nhom.png) | An nhận Group/Broadcast của Bình; Echo chỉ về Bình. |
| 5 | Bước 6 — reconnect | [buoc6-noilai.png](buoc6-noilai.png) | Reconnecting/Reconnected và ConnectionId mới. |
| 6 | Bước 7 — lỗi 1 | [buoc7-loi1.png](buoc7-loi1.png) | API tắt, HttpRequestException, actively refused/10061. |
| 7 | Bước 7 — lỗi 2 | [buoc7-loi2.png](buoc7-loi2.png) | Sai /hub/ping, HTTP 404. |
| 8 | Bước 7 — lỗi 3 | [buoc7-loi3.png](buoc7-loi3.png) | EchoRespone sai tên; không có Echo runtime, không exception. |
| 9 | Build cuối — bốn project | [build-cuoi.png](build-cuoi.png) | Hai solution cùng Build succeeded, 0 Warning(s), 0 Error(s). |
| 10 | EduSubmit — graded | [edusubmit-graded.png](edusubmit-graded.png) | Máy chủ Push; client nhận processing/graded rồi GET REST. |
| 11 | EduSubmit — HTTP | [edusubmit-http.png](edusubmit-http.png) | 200/201/400/404, Location, negotiate và GET graded. |
| 12 | EduSubmit — hai sinh viên | [edusubmit-hai-sinhvien.png](edusubmit-hai-sinhvien.png) | SV001 và SV002 chỉ nhận trạng thái của ID mình. |
| 13 | EduSubmit — hai ca mất push | [edusubmit-sai-event.png](edusubmit-sai-event.png) | Sai event và Watch muộn; hết thời gian chờ, REST vẫn graded. |
| 14 | Mở rộng — hai lượt nộp | [edusubmit-hai-luot-failed.png](edusubmit-hai-luot-failed.png) | Một kết nối, hai ID, graded + failed, cảnh báo đỏ và GET từng ID. |
| 15 | EduSubmit — hai lỗi kết nối | [edusubmit-api-tat.png](edusubmit-api-tat.png) | Sai hub trả 404; API tắt gây connection refused. |

Một số tên PNG khác trong thư mục giữ tương thích với các mốc của bài giảng và dùng chung một ảnh. README chỉ hiển thị 15 ảnh khác nhau để tránh lặp.

[Quay lại README](../README.md)
