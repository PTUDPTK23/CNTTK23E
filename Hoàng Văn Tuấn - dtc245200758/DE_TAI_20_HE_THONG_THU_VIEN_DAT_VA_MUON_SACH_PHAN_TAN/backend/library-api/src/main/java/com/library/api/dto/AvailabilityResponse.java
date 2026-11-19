package com.library.api.dto;

import java.util.UUID;

public record AvailabilityResponse(UUID bookId, long totalCopies, long availableCopies, long reservedCopies,
                                   long borrowedCopies, long otherCopies, boolean available) {
}
