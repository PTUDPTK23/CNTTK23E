package com.library.api.controller;

import com.library.api.dto.ApiResponse;
import com.library.api.dto.CopyResponse;
import com.library.api.dto.CopyStatusRequest;
import com.library.api.service.CopyService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("/api/copies")
@RequiredArgsConstructor
public class CopyController {
    private final CopyService copyService;

    @PutMapping("/{copyId}/status")
    public ApiResponse<CopyResponse> updateStatus(@PathVariable UUID copyId, @Valid @RequestBody CopyStatusRequest req) {
        return ApiResponse.ok("Đã cập nhật trạng thái bản sao", copyService.updateStatus(copyId, req.status()));
    }
}
