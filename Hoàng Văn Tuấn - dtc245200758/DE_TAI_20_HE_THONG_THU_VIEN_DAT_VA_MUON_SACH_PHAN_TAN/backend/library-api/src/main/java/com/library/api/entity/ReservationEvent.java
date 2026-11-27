package com.library.api.entity;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "reservation_events")
@Getter
@Setter
@NoArgsConstructor
public class ReservationEvent {
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;
    @Column(name = "reservation_id", nullable = false)
    private UUID reservationId;
    @Column(name = "old_status")
    private String oldStatus;
    @Column(name = "new_status", nullable = false)
    private String newStatus;
    private String note;
    @Column(name = "actor_type", nullable = false)
    private String actorType;
    @Column(name = "actor_id")
    private UUID actorId;
    @Column(name = "created_at", nullable = false, updatable = false)
    private Instant createdAt;

    @PrePersist
    void onCreate() {
        createdAt = Instant.now();
    }
}
