package com.library.api.dto;

import jakarta.validation.constraints.Size;

/** copyCode có thể bỏ trống: hệ thống tự sinh mã. */
public record CopyRequest(@Size(max = 50) String copyCode) {
}
