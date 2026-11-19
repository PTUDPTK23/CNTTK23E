package com.library.worker.repository;

import com.library.worker.entity.UserRef;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.UUID;

public interface UserRefRepository extends JpaRepository<UserRef, UUID> {
    boolean existsByIdAndStatus(UUID id, String status);
}
