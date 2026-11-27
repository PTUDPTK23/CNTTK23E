package com.library.api.dto;

import com.library.api.enums.ReservationStatus;

import java.util.UUID;

public record ReservationCreatedResponse(UUID reservationId, UUID requestId, ReservationStatus status) {
}
