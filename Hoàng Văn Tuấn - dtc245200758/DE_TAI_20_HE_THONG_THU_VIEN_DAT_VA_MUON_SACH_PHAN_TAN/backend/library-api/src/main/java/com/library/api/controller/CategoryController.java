package com.library.api.controller;

import com.library.api.dto.ApiResponse;
import com.library.api.dto.CategoryRequest;
import com.library.api.dto.CategoryResponse;
import com.library.api.service.CategoryService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/categories")
@RequiredArgsConstructor
public class CategoryController {
    private final CategoryService service;

    @GetMapping
    public ApiResponse<List<CategoryResponse>> list() {
        return ApiResponse.ok(service.list());
    }

    @PostMapping
    public ResponseEntity<ApiResponse<CategoryResponse>> create(@Valid @RequestBody CategoryRequest req) {
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok("Đã thêm thể loại", service.create(req)));
    }

    @PutMapping("/{id}")
    public ApiResponse<CategoryResponse> update(@PathVariable UUID id, @Valid @RequestBody CategoryRequest req) {
        return ApiResponse.ok("Đã cập nhật thể loại", service.update(id, req));
    }
}
