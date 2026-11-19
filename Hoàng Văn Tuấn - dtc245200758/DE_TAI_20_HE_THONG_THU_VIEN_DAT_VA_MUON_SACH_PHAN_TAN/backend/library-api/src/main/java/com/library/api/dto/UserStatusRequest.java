package com.library.api.dto;

import com.library.api.enums.UserStatus;
import jakarta.validation.constraints.NotNull;

public record UserStatusRequest(@NotNull(message = "Trạng thái không được để trống") UserStatus status) {
}
