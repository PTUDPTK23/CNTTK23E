package com.library.api.dto;

import com.library.api.enums.Role;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UserRequest(@NotBlank(message = "Họ tên không được để trống") @Size(max = 150) String fullName,
                          @NotBlank(message = "Email không được để trống") @Email(message = "Email không hợp lệ") @Size(max = 150) String email,
                          @Size(max = 20) String phone,
                          Role role) {
}
