package com.library.api.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CategoryRequest(@NotBlank(message = "Tên thể loại không được để trống") @Size(max = 100) String name,
                              @Size(max = 500) String description) {
}
