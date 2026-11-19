package com.library.worker.entity;

import com.library.worker.enums.CopyStatus;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "book_copies")
@Getter
@Setter
@NoArgsConstructor
public class BookCopy {
    @Id
    private UUID id;
    @Column(name = "book_id", nullable = false)
    private UUID bookId;
    @Column(name = "copy_code", nullable = false)
    private String copyCode;
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private CopyStatus status;
    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;
}
