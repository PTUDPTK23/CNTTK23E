package com.library.api.controller;

import com.library.api.dto.*;
import com.library.api.service.BookService;
import com.library.api.service.CopyService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/books")
@RequiredArgsConstructor
public class BookController {
    private final BookService bookService;
    private final CopyService copyService;

    @GetMapping
    public ApiResponse<PageResponse<BookResponse>> list(@RequestParam(defaultValue = "0") int page,
                                                         @RequestParam(defaultValue = "10") int size,
                                                         @RequestParam(required = false) String keyword,
                                                         @RequestParam(required = false) UUID categoryId,
                                                         @RequestParam(required = false) String author,
                                                         @RequestParam(required = false) String availability) {
        return ApiResponse.ok(bookService.search(keyword, categoryId, author, availability, page, size));
    }

    @GetMapping("/{id}")
    public ApiResponse<BookResponse> get(@PathVariable UUID id) {
        return ApiResponse.ok(bookService.get(id));
    }

    @PostMapping
    public ResponseEntity<ApiResponse<BookResponse>> create(@Valid @RequestBody BookRequest req) {
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok("Đã thêm sách", bookService.create(req)));
    }

    @PutMapping("/{id}")
    public ApiResponse<BookResponse> update(@PathVariable UUID id, @Valid @RequestBody BookRequest req) {
        return ApiResponse.ok("Đã cập nhật sách", bookService.update(id, req));
    }

    @DeleteMapping("/{id}")
    public ApiResponse<Void> delete(@PathVariable UUID id) {
        bookService.delete(id);
        return ApiResponse.ok("Đã xóa sách", null);
    }

    @GetMapping("/{id}/availability")
    public ApiResponse<AvailabilityResponse> availability(@PathVariable UUID id) {
        return ApiResponse.ok(bookService.availability(id));
    }

    @GetMapping("/{bookId}/copies")
    public ApiResponse<List<CopyResponse>> copies(@PathVariable UUID bookId) {
        return ApiResponse.ok(copyService.list(bookId));
    }

    @PostMapping("/{bookId}/copies")
    public ResponseEntity<ApiResponse<CopyResponse>> addCopy(@PathVariable UUID bookId, @Valid @RequestBody(required = false) CopyRequest req) {
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok("Đã thêm bản sao", copyService.add(bookId, req)));
    }
}
