package com.library.api.entity;

import com.library.api.enums.CopyStatus;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.UUID;

@Entity
@Table(name = "book_copies")
@Getter
@Setter
@NoArgsConstructor
public class BookCopy extends AuditableEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;
    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "book_id", nullable = false)
    private Book book;
    @Column(name = "copy_code", nullable = false)
    private String copyCode;
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private CopyStatus status = CopyStatus.AVAILABLE;
}
