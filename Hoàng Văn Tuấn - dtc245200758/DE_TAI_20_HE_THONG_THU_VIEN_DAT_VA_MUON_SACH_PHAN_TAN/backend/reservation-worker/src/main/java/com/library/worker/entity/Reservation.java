package com.library.worker.entity;

import com.library.worker.enums.ReservationStatus;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.Instant;
import java.util.UUID;

/** Chỉ ánh xạ các cột Worker cần (cùng bảng reservations với Library API). */
@Entity
@Table(name = "reservations")
@Getter
@Setter
@NoArgsConstructor
public class Reservation {
    @Id
    private UUID id;
    @Column(name = "user_id", nullable = false)
    private UUID userId;
    @Column(name = "book_id", nullable = false)
    private UUID bookId;
    @Column(name = "book_copy_id")
    private UUID bookCopyId;
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private ReservationStatus status;
    @Column(name = "processed_at")
    private Instant processedAt;
    @Column(name = "rejection_reason")
    private String rejectionReason;
    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;
}
