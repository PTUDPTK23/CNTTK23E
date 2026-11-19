package com.library.worker.dto;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.time.Instant;
import java.util.UUID;

@JsonIgnoreProperties(ignoreUnknown = true)
public record ReservationRequestedEvent(UUID eventId, UUID reservationId, UUID userId, UUID bookId,
                                        String eventType, Instant createdAt) {
}
