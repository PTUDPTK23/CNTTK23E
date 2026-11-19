package com.library.api.repository;

import com.library.api.entity.OutboxMessage;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.UUID;

public interface OutboxRepository extends JpaRepository<OutboxMessage, UUID> {
    /** SKIP LOCKED: nhiều instance API có thể chạy publisher mà không gửi trùng. */
    @Query(value = "SELECT * FROM message_outbox WHERE status = 'PENDING' ORDER BY created_at LIMIT :limit FOR UPDATE SKIP LOCKED",
            nativeQuery = true)
    List<OutboxMessage> lockPending(@Param("limit") int limit);

    long countByStatus(String status);
}
