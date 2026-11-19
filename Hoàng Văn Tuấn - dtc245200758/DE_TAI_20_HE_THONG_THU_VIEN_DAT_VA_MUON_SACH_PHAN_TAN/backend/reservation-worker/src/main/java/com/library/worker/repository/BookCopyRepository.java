package com.library.worker.repository;

import com.library.worker.entity.BookCopy;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.Optional;
import java.util.UUID;

public interface BookCopyRepository extends JpaRepository<BookCopy, UUID> {
    /**
     * Chọn và KHÓA một bản sao còn trống. SKIP LOCKED: worker song song sẽ bỏ qua bản đã bị worker khác khóa
     * và lấy bản kế tiếp => hai reservation không bao giờ nhận cùng một bản sao, và không chờ nhau.
     */
    @Query(value = "SELECT * FROM book_copies WHERE book_id = :bookId AND status = 'AVAILABLE' "
            + "ORDER BY copy_code LIMIT 1 FOR UPDATE SKIP LOCKED", nativeQuery = true)
    Optional<BookCopy> lockFirstAvailable(@Param("bookId") UUID bookId);
}
