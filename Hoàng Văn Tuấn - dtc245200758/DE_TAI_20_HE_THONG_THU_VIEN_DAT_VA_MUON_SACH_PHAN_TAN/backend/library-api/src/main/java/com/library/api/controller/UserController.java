package com.library.api.controller;

import com.library.api.dto.*;
import com.library.api.enums.ReservationStatus;
import com.library.api.service.ReservationService;
import com.library.api.service.UserService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("/api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserService userService;
    private final ReservationService reservationService;

    private boolean isStaff(Authentication authentication) {
        return authentication.getAuthorities().stream()
                .map(GrantedAuthority::getAuthority)
                .anyMatch(role -> role.equals("ROLE_ADMIN") || role.equals("ROLE_LIBRARIAN"));
    }

    @GetMapping
    public ApiResponse<PageResponse<UserResponse>> list(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "100") int size) {
        return ApiResponse.ok(userService.list(page, size));
    }

    @GetMapping("/{id}")
    public ApiResponse<UserResponse> get(@PathVariable UUID id) {
        return ApiResponse.ok(userService.get(id));
    }

    @PostMapping
    public ResponseEntity<ApiResponse<UserResponse>> create(@Valid @RequestBody UserRequest req) {
        return ResponseEntity.status(HttpStatus.CREATED)
                .body(ApiResponse.ok("Đã tạo người dùng", userService.create(req)));
    }

    @PutMapping("/{id}/status")
    public ApiResponse<UserResponse> setStatus(
            @PathVariable UUID id, @Valid @RequestBody UserStatusRequest req) {
        return ApiResponse.ok("Đã cập nhật trạng thái tài khoản",
                userService.setStatus(id, req.status()));
    }

    @GetMapping("/{userId}/reservations")
    public ApiResponse<PageResponse<ReservationResponse>> reservations(
            @PathVariable UUID userId,
            @RequestParam(required = false) ReservationStatus status,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "100") int size,
            Authentication authentication) {
        return ApiResponse.ok(reservationService.listByUser(
                userId, status, page, size, authentication.getName(), isStaff(authentication)));
    }
}