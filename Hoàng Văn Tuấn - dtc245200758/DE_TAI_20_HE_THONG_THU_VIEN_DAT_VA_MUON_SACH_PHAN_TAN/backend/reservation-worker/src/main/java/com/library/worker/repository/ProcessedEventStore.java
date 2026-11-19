package com.library.worker.repository;

import lombok.RequiredArgsConstructor;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

import java.util.UUID;

/** Bảng processed_events: ghi nhận event đã xử lý (idempotency). */
@Repository
@RequiredArgsConstructor
public class ProcessedEventStore {
    private final JdbcTemplate jdbc;

    /**
     * @return true nếu event chưa từng xử lý (đã ghi nhận), false nếu là bản trùng.
     * ON CONFLICT DO NOTHING + khóa unique: hai consumer nhận cùng event, chỉ một bên được true.
     * Bản ghi nằm trong cùng transaction với nghiệp vụ nên nếu xử lý lỗi và rollback, event vẫn được retry.
     */
    public boolean markProcessed(UUID eventId, UUID reservationId) {
        return jdbc.update("INSERT INTO processed_events (event_id, reservation_id) VALUES (?, ?) ON CONFLICT (event_id) DO NOTHING",
                eventId, reservationId) == 1;
    }
}
