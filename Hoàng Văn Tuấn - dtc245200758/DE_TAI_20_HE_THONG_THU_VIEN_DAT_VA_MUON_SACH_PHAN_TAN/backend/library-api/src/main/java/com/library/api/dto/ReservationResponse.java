package com.library.api.dto;

import com.library.api.enums.ReservationStatus;

import java.time.Instant;
import java.util.UUID;

public record ReservationResponse(UUID id, UUID requestId, UUID userId, String userName, UUID bookId, String bookTitle,
                                  String bookAuthor, UUID copyId, String copyCode, ReservationStatus status,
                                  Instant requestedAt, Instant processedAt, Instant borrowedAt, Instant dueDate,
                                  Instant returnedAt, String rejectionReason, Instant createdAt, Instant updatedAt) {
}
