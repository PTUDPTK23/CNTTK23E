package com.library.api.dto;

/** Thống kê số bản sao theo thể loại (dùng cho biểu đồ trang quản trị). */
public record CategoryStatResponse(String category, long totalCopies, long availableCopies) {
}
