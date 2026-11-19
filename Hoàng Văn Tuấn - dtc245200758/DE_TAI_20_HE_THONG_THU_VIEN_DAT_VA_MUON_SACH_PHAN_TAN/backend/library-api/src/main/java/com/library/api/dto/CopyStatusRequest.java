package com.library.api.dto;

import com.library.api.enums.CopyStatus;
import jakarta.validation.constraints.NotNull;

public record CopyStatusRequest(@NotNull(message = "Trạng thái không được để trống") CopyStatus status) {
}
