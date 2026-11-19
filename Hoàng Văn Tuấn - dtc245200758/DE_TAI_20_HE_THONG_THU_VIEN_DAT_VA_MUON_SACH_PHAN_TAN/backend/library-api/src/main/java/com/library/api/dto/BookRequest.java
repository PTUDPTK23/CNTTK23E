package com.library.api.dto;

import jakarta.validation.constraints.*;

import java.util.UUID;

public record BookRequest(
        @NotBlank(message = "Tên sách không được để trống") @Size(max = 255) String title,
        @NotBlank(message = "Tác giả không được để trống") @Size(max = 150) String author,
        @Size(max = 20, message = "ISBN tối đa 20 ký tự") String isbn,
        @Size(max = 150) String publisher,
        @Min(value = 1000, message = "Năm xuất bản không hợp lệ") @Max(value = 2100, message = "Năm xuất bản không hợp lệ") Integer publicationYear,
        @NotNull(message = "Phải chọn thể loại") UUID categoryId,
        String description,
        @Size(max = 500) String coverImage) {
}
