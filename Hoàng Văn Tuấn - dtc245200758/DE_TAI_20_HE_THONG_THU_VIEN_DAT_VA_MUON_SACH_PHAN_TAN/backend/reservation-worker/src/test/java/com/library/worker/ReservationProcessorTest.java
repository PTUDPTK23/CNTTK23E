package com.library.worker;

import com.library.worker.dto.ReservationRequestedEvent;
import com.library.worker.entity.BookCopy;
import com.library.worker.entity.Reservation;
import com.library.worker.enums.CopyStatus;
import com.library.worker.enums.ReservationStatus;
import com.library.worker.repository.*;
import com.library.worker.service.ReservationProcessor;
import com.library.worker.service.ReservationProcessor.Outcome;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Instant;
import java.util.Optional;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class ReservationProcessorTest {
    @Mock ProcessedEventStore processedEvents;
    @Mock ReservationRepository reservationRepo;
    @Mock BookCopyRepository copyRepo;
    @Mock UserRefRepository userRepo;
    @Mock BookRefRepository bookRepo;
    @Mock ReservationHistoryWriter history;
    @InjectMocks ReservationProcessor processor;

    private ReservationRequestedEvent event;
    private Reservation reservation;

    @BeforeEach
    void setUp() {
        UUID resId = UUID.randomUUID();
        event = new ReservationRequestedEvent(UUID.randomUUID(), resId, UUID.randomUUID(), UUID.randomUUID(),
                "RESERVATION_REQUESTED", Instant.now());
        reservation = new Reservation();
        reservation.setId(resId);
        reservation.setUserId(event.userId());
        reservation.setBookId(event.bookId());
        reservation.setStatus(ReservationStatus.PENDING);
    }

    private BookCopy copy() {
        BookCopy c = new BookCopy();
        c.setId(UUID.randomUUID());
        c.setCopyCode("C-01");
        c.setStatus(CopyStatus.AVAILABLE);
        return c;
    }

    @Test
    void reservesCopyWhenAvailable() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));
        when(userRepo.existsByIdAndStatus(any(), anyString())).thenReturn(true);
        when(bookRepo.existsByIdAndActiveTrue(any())).thenReturn(true);
        BookCopy c = copy();
        when(copyRepo.lockFirstAvailable(event.bookId())).thenReturn(Optional.of(c));

        assertEquals(Outcome.RESERVED, processor.process(event));
        assertEquals(ReservationStatus.RESERVED, reservation.getStatus());
        assertEquals(CopyStatus.RESERVED, c.getStatus());
        assertEquals(c.getId(), reservation.getBookCopyId());
        verify(history).record(any(), eq("PENDING"), eq("RESERVED"), anyString());
    }

    @Test
    void rejectsWhenNoCopyAvailable() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));
        when(userRepo.existsByIdAndStatus(any(), anyString())).thenReturn(true);
        when(bookRepo.existsByIdAndActiveTrue(any())).thenReturn(true);
        when(copyRepo.lockFirstAvailable(event.bookId())).thenReturn(Optional.empty());

        assertEquals(Outcome.REJECTED, processor.process(event));
        assertEquals(ReservationStatus.REJECTED, reservation.getStatus());
        assertNotNull(reservation.getRejectionReason());
        assertNull(reservation.getBookCopyId());
    }

    @Test
    void ignoresDuplicateEvent() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(false);

        assertEquals(Outcome.DUPLICATE, processor.process(event));
        verifyNoInteractions(reservationRepo, copyRepo, history);
    }

    @Test
    void skipsReservationThatIsNoLongerPending() {
        reservation.setStatus(ReservationStatus.CANCELLED);
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));

        assertEquals(Outcome.SKIPPED, processor.process(event));
        verifyNoInteractions(copyRepo);
        assertEquals(ReservationStatus.CANCELLED, reservation.getStatus());
    }


    @Test
    void rejectsWhenUserIsInactive() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));
        when(userRepo.existsByIdAndStatus(any(), eq("ACTIVE"))).thenReturn(false);

        assertEquals(Outcome.REJECTED, processor.process(event));
        assertEquals(ReservationStatus.REJECTED, reservation.getStatus());
        assertEquals("Người dùng không tồn tại hoặc đã bị khóa", reservation.getRejectionReason());
        verify(bookRepo, never()).existsByIdAndActiveTrue(any());
        verify(copyRepo, never()).lockFirstAvailable(any());
        verify(history).record(any(), eq("PENDING"), eq("REJECTED"), anyString());
    }

    @Test
    void rejectsWhenBookIsInactive() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));
        when(userRepo.existsByIdAndStatus(any(), eq("ACTIVE"))).thenReturn(true);
        when(bookRepo.existsByIdAndActiveTrue(any())).thenReturn(false);

        assertEquals(Outcome.REJECTED, processor.process(event));
        assertEquals(ReservationStatus.REJECTED, reservation.getStatus());
        assertEquals("Đầu sách không còn được phục vụ", reservation.getRejectionReason());
        verify(copyRepo, never()).lockFirstAvailable(any());
        verify(history).record(any(), eq("PENDING"), eq("REJECTED"), anyString());
    }

    @Test
    void recordsHistoryWhenReservationIsRejectedBecauseAllCopiesAreTaken() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(event.reservationId())).thenReturn(Optional.of(reservation));
        when(userRepo.existsByIdAndStatus(any(), eq("ACTIVE"))).thenReturn(true);
        when(bookRepo.existsByIdAndActiveTrue(any())).thenReturn(true);
        when(copyRepo.lockFirstAvailable(event.bookId())).thenReturn(Optional.empty());

        assertEquals(Outcome.REJECTED, processor.process(event));
        assertEquals(ReservationStatus.REJECTED, reservation.getStatus());
        assertEquals("Hết bản sách khả dụng", reservation.getRejectionReason());
        assertNotNull(reservation.getProcessedAt());
        verify(history).record(event.reservationId(), "PENDING", "REJECTED", "Hết bản sách khả dụng");
    }

    @Test
    void throwsWhenReservationMissingSoMessageIsRetried() {
        when(processedEvents.markProcessed(any(), any())).thenReturn(true);
        when(reservationRepo.findByIdForUpdate(any())).thenReturn(Optional.empty());

        assertThrows(IllegalStateException.class, () -> processor.process(event));
    }
}
