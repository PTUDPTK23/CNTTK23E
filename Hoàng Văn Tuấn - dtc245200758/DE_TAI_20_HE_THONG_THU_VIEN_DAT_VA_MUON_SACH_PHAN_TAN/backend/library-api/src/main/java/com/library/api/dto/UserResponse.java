package com.library.api.dto;

import com.library.api.enums.Role;
import com.library.api.enums.UserStatus;

import java.time.Instant;
import java.util.UUID;

/** Không trả password_hash hay thông tin nhạy cảm. */
public record UserResponse(UUID id, String fullName, String email, String phone, Role role, UserStatus status, Instant createdAt) {
}
