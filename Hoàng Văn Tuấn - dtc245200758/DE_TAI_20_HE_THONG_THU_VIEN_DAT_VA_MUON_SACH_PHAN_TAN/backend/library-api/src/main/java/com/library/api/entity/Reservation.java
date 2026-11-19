package com.library.api.entity;

import com.library.api.enums.ReservationStatus;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "reservations")
@Getter
@Setter
@NoArgsConstructor
public class Reservation extends AuditableEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;
    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;
    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "book_id", nullable = false)
    private Book book;
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "book_copy_id")
    private BookCopy bookCopy;
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private ReservationStatus status = ReservationStatus.PENDING;
    @Column(name = "request_id", nullable = false)
    private UUID requestId;
    @Column(name = "requested_at", nullable = false)
    private Instant requestedAt;
    @Column(name = "processed_at")
    private Instant processedAt;
    @Column(name = "borrowed_at")
    private Instant borrowedAt;
    @Column(name = "due_date")
    private Instant dueDate;
    @Column(name = "returned_at")
    private Instant returnedAt;
    @Column(name = "rejection_reason")
    private String rejectionReason;
}
