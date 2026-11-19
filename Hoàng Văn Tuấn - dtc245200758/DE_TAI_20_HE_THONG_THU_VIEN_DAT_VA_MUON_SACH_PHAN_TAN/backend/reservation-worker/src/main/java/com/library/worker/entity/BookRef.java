package com.library.worker.entity;

import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.util.UUID;

/** Chỉ đọc: kiểm tra đầu sách còn hoạt động. */
@Entity
@Table(name = "books")
@Getter
@NoArgsConstructor
public class BookRef {
    @Id
    private UUID id;
    private boolean active;
}
