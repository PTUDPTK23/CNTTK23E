package com.library.api.repository;

import com.library.api.entity.BookCopy;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.Collection;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface BookCopyRepository extends JpaRepository<BookCopy, UUID> {
    List<BookCopy> findByBookIdOrderByCopyCode(UUID bookId);
    boolean existsByCopyCode(String copyCode);
    long countByBookId(UUID bookId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select c from BookCopy c where c.id = :id")
    Optional<BookCopy> findByIdForUpdate(@Param("id") UUID id);

    @Query("select c.status, count(c) from BookCopy c where c.book.id = :bookId group by c.status")
    List<Object[]> countByStatus(@Param("bookId") UUID bookId);

    @Query("select c.book.id, c.status, count(c) from BookCopy c where c.book.id in :ids group by c.book.id, c.status")
    List<Object[]> countByBooks(@Param("ids") Collection<UUID> ids);

    @Query("select c.status, count(c) from BookCopy c group by c.status")
    List<Object[]> countAllByStatus();

    @Query("select c.book.category.name, c.status, count(c) from BookCopy c where c.book.active = true group by c.book.category.name, c.status")
    List<Object[]> countByCategoryAndStatus();
}
