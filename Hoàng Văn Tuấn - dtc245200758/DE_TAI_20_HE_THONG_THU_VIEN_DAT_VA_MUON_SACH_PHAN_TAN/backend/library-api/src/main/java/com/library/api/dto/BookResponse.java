package com.library.api.dto;

import java.time.Instant;
import java.util.UUID;

public record BookResponse(UUID id, String isbn, String title, String author, String publisher, Integer publicationYear,
                           UUID categoryId, String categoryName, String description, String coverImage,
                           long totalCopies, long availableCopies, long reservedCopies, long borrowedCopies,
                           Instant createdAt) {
}
