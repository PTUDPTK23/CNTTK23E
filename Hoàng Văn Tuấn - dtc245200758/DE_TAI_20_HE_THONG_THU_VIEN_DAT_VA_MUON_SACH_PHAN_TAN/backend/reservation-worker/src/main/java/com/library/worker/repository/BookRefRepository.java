package com.library.worker.repository;

import com.library.worker.entity.BookRef;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.UUID;

public interface BookRefRepository extends JpaRepository<BookRef, UUID> {
    boolean existsByIdAndActiveTrue(UUID id);
}
