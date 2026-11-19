package com.library.api.dto;

import java.time.Instant;
import java.util.UUID;

public record ReservationEventResponse(UUID id, String oldStatus, String newStatus, String note, String actorType, Instant createdAt) {
}
