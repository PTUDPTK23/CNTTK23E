package com.library.api.repository;

import com.library.api.entity.Book;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

import java.util.Optional;
import java.util.UUID;

public interface BookRepository extends JpaRepository<Book, UUID>, JpaSpecificationExecutor<Book> {
    Optional<Book> findByIdAndActiveTrue(UUID id);
    boolean existsByIsbn(String isbn);
    boolean existsByIsbnAndIdNot(String isbn, UUID id);
    long countByActiveTrue();

    @Override
    @EntityGraph(attributePaths = "category")
    Page<Book> findAll(Specification<Book> spec, Pageable pageable);
}
