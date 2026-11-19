package com.library.worker.entity;

import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.util.UUID;

/** Chỉ đọc: kiểm tra người dùng còn hợp lệ (ACTIVE). */
@Entity
@Table(name = "users")
@Getter
@NoArgsConstructor
public class UserRef {
    @Id
    private UUID id;
    private String status;
}
