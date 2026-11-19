package com.library.worker.repository;

import lombok.RequiredArgsConstructor;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

import java.util.UUID;

/** Ghi lịch sử đổi trạng thái vào reservation_events. */
@Repository
@RequiredArgsConstructor
public class ReservationHistoryWriter {
    private final JdbcTemplate jdbc;

    public void record(UUID reservationId, String oldStatus, String newStatus, String note) {
        jdbc.update("INSERT INTO reservation_events (reservation_id, old_status, new_status, note, actor_type) VALUES (?, ?, ?, ?, 'WORKER')",
                reservationId, oldStatus, newStatus, note);
    }
}
