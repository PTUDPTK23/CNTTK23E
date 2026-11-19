package com.library.api.controller;

import com.library.api.dto.ApiResponse;
import com.library.api.dto.CategoryStatResponse;
import com.library.api.dto.PipelineResponse;
import com.library.api.dto.StatsResponse;
import com.library.api.service.StatsService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api")
@RequiredArgsConstructor
public class SystemController {
    private final StatsService statsService;

    @GetMapping("/health")
    public ApiResponse<Map<String, String>> health() {
        return ApiResponse.ok(Map.of("status", "UP"));
    }

    @GetMapping("/stats")
    public ApiResponse<StatsResponse> stats() {
        return ApiResponse.ok(statsService.stats());
    }

    @GetMapping("/stats/categories")
    public ApiResponse<List<CategoryStatResponse>> statsByCategory() {
        return ApiResponse.ok(statsService.byCategory());
    }

    /** Tình trạng Outbox -> RabbitMQ -> Worker (số message chờ, số consumer, DLQ). */
    @GetMapping("/system/pipeline")
    public ApiResponse<PipelineResponse> pipeline() {
        return ApiResponse.ok(statsService.pipeline());
    }
}
