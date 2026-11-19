package com.library.api.dto;

import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record ReservationRequest(@NotNull(message = "userId không được để trống") UUID userId,
                                 @NotNull(message = "bookId không được để trống") UUID bookId) {
}
