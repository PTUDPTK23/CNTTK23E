package com.library.api;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.library.api.dto.ReservationCreatedResponse;
import com.library.api.dto.ReservationRequest;
import com.library.api.entity.Book;
import com.library.api.entity.OutboxMessage;
import com.library.api.entity.Reservation;
import com.library.api.entity.User;
import com.library.api.enums.ReservationStatus;
import com.library.api.enums.UserStatus;
import com.library.api.exception.ApiException;
import com.library.api.repository.*;
import com.library.api.service.ReservationService;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Spy;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;

import java.util.Optional;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class ReservationServiceTest {
    @Mock ReservationRepository reservationRepo;
    @Mock UserRepository userRepo;
    @Mock BookRepository bookRepo;
    @Mock BookCopyRepository copyRepo;
    @Mock ReservationEventRepository eventRepo;
    @Mock OutboxRepository outboxRepo;
    @Spy ObjectMapper objectMapper = new ObjectMapper().findAndRegisterModules();
    @InjectMocks ReservationService service;

    private User user() {
        User u = new User();
        u.setId(UUID.randomUUID());
        u.setFullName("Test");
        u.setEmail("test@library.local");
        u.setStatus(UserStatus.ACTIVE);
        return u;
    }

    private Book book() {
        Book b = new Book();
        b.setId(UUID.randomUUID());
        b.setTitle("Sách");
        return b;
    }

    @Test
    void createSavesPendingReservationAndOutboxInSameFlow() {
        User u = user();
        Book b = book();
        when(userRepo.findByEmailIgnoreCase("test@library.local")).thenReturn(Optional.of(u));
        when(bookRepo.findByIdAndActiveTrue(b.getId())).thenReturn(Optional.of(b));
        when(reservationRepo.existsByUserIdAndBookIdAndStatusIn(any(), any(), any())).thenReturn(false);
        when(reservationRepo.saveAndFlush(any(Reservation.class))).thenAnswer(i -> i.getArgument(0));

        ReservationCreatedResponse res = service.create(new ReservationRequest(u.getId(), b.getId()), "test@library.local");

        assertEquals(ReservationStatus.PENDING, res.status());
        ArgumentCaptor<OutboxMessage> cap = ArgumentCaptor.forClass(OutboxMessage.class);
        verify(outboxRepo).save(cap.capture());
        assertEquals(OutboxMessage.PENDING, cap.getValue().getStatus());
        assertTrue(cap.getValue().getPayload().contains("RESERVATION_REQUESTED"));
    }

    @Test
    void createRejectsDuplicateActiveReservationWith409() {
        User u = user();
        Book b = book();
        when(userRepo.findByEmailIgnoreCase("test@library.local")).thenReturn(Optional.of(u));
        when(bookRepo.findByIdAndActiveTrue(b.getId())).thenReturn(Optional.of(b));
        when(reservationRepo.existsByUserIdAndBookIdAndStatusIn(any(), any(), any())).thenReturn(true);

        ApiException ex = assertThrows(ApiException.class, () -> service.create(new ReservationRequest(u.getId(), b.getId()), "test@library.local"));
        assertEquals(HttpStatus.CONFLICT, ex.getStatus());
        verify(outboxRepo, never()).save(any());
    }

    @Test
    void createFailsWhenUserNotFound() {
        UUID id = UUID.randomUUID();
        when(userRepo.findByEmailIgnoreCase("missing@library.local")).thenReturn(Optional.empty());
        ApiException ex = assertThrows(ApiException.class, () -> service.create(new ReservationRequest(id, UUID.randomUUID()), "missing@library.local"));
        assertEquals(HttpStatus.NOT_FOUND, ex.getStatus());
    }
}
