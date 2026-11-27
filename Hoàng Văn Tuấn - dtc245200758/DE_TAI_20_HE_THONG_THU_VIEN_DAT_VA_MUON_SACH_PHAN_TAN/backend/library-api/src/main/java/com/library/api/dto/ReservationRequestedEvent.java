package com.library.api.dto;

import java.time.Instant;
import java.util.UUID;

/** Nội dung message gửi RabbitMQ. Chỉ chứa định danh, không có dữ liệu nhạy cảm. */
public record ReservationRequestedEvent(UUID eventId, UUID reservationId, UUID userId, UUID bookId,
                                        String eventType, Instant createdAt) {
    public static final String TYPE = "RESERVATION_REQUESTED";
}
