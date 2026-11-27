package com.library.api.repository;

import com.library.api.entity.Reservation;
import com.library.api.enums.ReservationStatus;
import jakarta.persistence.LockModeType;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.Instant;
import java.util.Collection;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface ReservationRepository extends JpaRepository<Reservation, UUID> {
    /** Khóa dòng reservation: tuần tự hóa với Worker khi cùng đổi trạng thái. */
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select r from Reservation r where r.id = :id")
    Optional<Reservation> findByIdForUpdate(@Param("id") UUID id);

    @EntityGraph(attributePaths = {"user", "book", "bookCopy"})
    Optional<Reservation> findWithDetailsById(UUID id);

    @EntityGraph(attributePaths = {"user", "book", "bookCopy"})
    Page<Reservation> findByUserId(UUID userId, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "book", "bookCopy"})
    Page<Reservation> findByUserIdAndStatus(UUID userId, ReservationStatus status, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "book", "bookCopy"})
    Page<Reservation> findByStatus(ReservationStatus status, Pageable pageable);

    @Override
    @EntityGraph(attributePaths = {"user", "book", "bookCopy"})
    Page<Reservation> findAll(Pageable pageable);

    boolean existsByUserIdAndBookIdAndStatusIn(UUID userId, UUID bookId, Collection<ReservationStatus> statuses);
    boolean existsByBookIdAndStatusIn(UUID bookId, Collection<ReservationStatus> statuses);
    long countByStatus(ReservationStatus status);
    long countByStatusAndDueDateBefore(ReservationStatus status, Instant cutoff);

    @Query("select r.status, count(r) from Reservation r group by r.status")
    List<Object[]> countGroupedByStatus();

    /** Các yêu cầu đã giữ sách nhưng quá hạn đến nhận (dùng cho tác vụ tự động hủy). */
    @Query("select r.id from Reservation r where r.status = :status and r.processedAt < :cutoff")
    List<UUID> findStaleIds(@Param("status") ReservationStatus status, @Param("cutoff") Instant cutoff);
}
