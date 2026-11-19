package com.library.api.controller;

import com.library.api.dto.*;
import com.library.api.enums.ReservationStatus;
import com.library.api.service.ReservationService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/reservations")
@RequiredArgsConstructor
public class ReservationController {

    private final ReservationService service;

    private boolean isStaff(Authentication authentication) {
        return authentication.getAuthorities().stream()
                .map(GrantedAuthority::getAuthority)
                .anyMatch(role -> role.equals("ROLE_ADMIN") || role.equals("ROLE_LIBRARIAN"));
    }

    /** Tao PENDING + Outbox; danh tinh nguoi dung lay tu JWT. */
    @PostMapping
    public ResponseEntity<ApiResponse<ReservationCreatedResponse>> create(
            @Valid @RequestBody ReservationRequest req,
            Authentication authentication) {
        return ResponseEntity.status(HttpStatus.ACCEPTED)
                .body(ApiResponse.ok("Yêu cầu đặt mượn đã được tiếp nhận.",
                        service.create(req, authentication.getName())));
    }

    /** Danh sach tat ca chi danh cho nhan vien thu vien. */
    @GetMapping
    public ApiResponse<PageResponse<ReservationResponse>> listAll(
            @RequestParam(required = false) ReservationStatus status,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "50") int size) {
        return ApiResponse.ok(service.listAll(status, page, size));
    }

    @GetMapping("/{id}")
    public ApiResponse<ReservationResponse> get(
            @PathVariable UUID id, Authentication authentication) {
        return ApiResponse.ok(service.get(id, authentication.getName(), isStaff(authentication)));
    }

    @GetMapping("/{id}/events")
    public ApiResponse<List<ReservationEventResponse>> events(
            @PathVariable UUID id, Authentication authentication) {
        return ApiResponse.ok(service.events(id, authentication.getName(), isStaff(authentication)));
    }

    @PostMapping("/{id}/cancel")
    public ApiResponse<ReservationResponse> cancel(
            @PathVariable UUID id, Authentication authentication) {
        return ApiResponse.ok("Đã hủy yêu cầu",
                service.cancel(id, authentication.getName(), isStaff(authentication)));
    }

    @PostMapping("/{id}/borrow")
    public ApiResponse<ReservationResponse> borrow(@PathVariable UUID id) {
        return ApiResponse.ok("Đã xác nhận giao sách", service.borrow(id));
    }

    @PostMapping("/{id}/return")
    public ApiResponse<ReservationResponse> giveBack(@PathVariable UUID id) {
        return ApiResponse.ok("Đã xác nhận trả sách", service.giveBack(id));
    }
}