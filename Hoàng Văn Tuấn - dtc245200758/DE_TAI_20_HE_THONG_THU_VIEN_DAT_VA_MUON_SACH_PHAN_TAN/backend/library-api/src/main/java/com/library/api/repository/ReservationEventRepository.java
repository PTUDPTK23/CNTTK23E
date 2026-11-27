package com.library.api.repository;

import com.library.api.entity.ReservationEvent;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.UUID;

public interface ReservationEventRepository extends JpaRepository<ReservationEvent, UUID> {
    List<ReservationEvent> findByReservationIdOrderByCreatedAtAsc(UUID reservationId);
}
