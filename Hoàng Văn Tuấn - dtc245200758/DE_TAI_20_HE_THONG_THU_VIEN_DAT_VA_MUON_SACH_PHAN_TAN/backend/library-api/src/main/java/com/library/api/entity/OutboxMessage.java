package com.library.api.entity;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.Instant;
import java.util.UUID;

/** Bản ghi Outbox: được lưu cùng transaction với reservation, sau đó publisher gửi sang RabbitMQ. */
@Entity
@Table(name = "message_outbox")
@Getter
@Setter
@NoArgsConstructor
public class OutboxMessage {
    public static final String PENDING = "PENDING";
    public static final String PUBLISHED = "PUBLISHED";

    @Id
    private UUID id; // chính là eventId
    @Column(name = "aggregate_id", nullable = false)
    private UUID aggregateId;
    @Column(name = "event_type", nullable = false)
    private String eventType;
    @Column(nullable = false, columnDefinition = "text")
    private String payload;
    @Column(nullable = false)
    private String status = PENDING;
    @Column(nullable = false)
    private int attempts;
    @Column(name = "last_error")
    private String lastError;
    @Column(name = "created_at", nullable = false, updatable = false)
    private Instant createdAt = Instant.now();
    @Column(name = "published_at")
    private Instant publishedAt;

    public OutboxMessage(UUID id, UUID aggregateId, String eventType, String payload) {
        this.id = id;
        this.aggregateId = aggregateId;
        this.eventType = eventType;
        this.payload = payload;
    }
}
