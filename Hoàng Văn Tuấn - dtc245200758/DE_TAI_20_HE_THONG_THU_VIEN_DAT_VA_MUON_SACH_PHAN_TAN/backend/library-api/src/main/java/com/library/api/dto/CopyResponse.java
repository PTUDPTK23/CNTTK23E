package com.library.api.dto;

import com.library.api.enums.CopyStatus;

import java.time.Instant;
import java.util.UUID;

public record CopyResponse(UUID id, UUID bookId, String copyCode, CopyStatus status, Instant createdAt, Instant updatedAt) {
}
