package com.library.api.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.library.api.dto.*;
import com.library.api.entity.*;
import com.library.api.enums.CopyStatus;
import com.library.api.enums.ReservationStatus;
import com.library.api.enums.UserStatus;
import com.library.api.exception.ApiException;
import com.library.api.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Nghiệp vụ đặt mượn phía API. API CHỈ tạo yêu cầu (PENDING) + ghi Outbox;
 * việc phân bổ bản sao do Reservation Worker thực hiện qua RabbitMQ.
 */
@Service
@RequiredArgsConstructor
public class ReservationService {
    private final ReservationRepository reservationRepo;
    private final UserRepository userRepo;
    private final BookRepository bookRepo;
    private final BookCopyRepository copyRepo;
    private final ReservationEventRepository eventRepo;
    private final OutboxRepository outboxRepo;
    private final ObjectMapper objectMapper;

    @Value("${app.loan.days:14}")
    private int loanDays;

    /** Bước 1-3 của luồng đặt mượn: kiểm tra -> lưu PENDING + Outbox trong CÙNG một transaction. */
    @Transactional
    public ReservationCreatedResponse create(ReservationRequest req, String authenticatedEmail) {
        User user = userRepo.findByEmailIgnoreCase(authenticatedEmail)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng"));
        if (user.getStatus() != UserStatus.ACTIVE) throw ApiException.forbidden("Tài khoản đang bị khóa");
        Book book = bookRepo.findByIdAndActiveTrue(req.bookId()).orElseThrow(() -> ApiException.notFound("Không tìm thấy sách"));
        if (reservationRepo.existsByUserIdAndBookIdAndStatusIn(user.getId(), book.getId(), ReservationStatus.ACTIVE)) {
            throw ApiException.conflict("Bạn đã có yêu cầu đang xử lý hoặc đang mượn cuốn sách này");
        }

        Reservation r = new Reservation();
        r.setUser(user);
        r.setBook(book);
        r.setStatus(ReservationStatus.PENDING);
        r.setRequestId(UUID.randomUUID());
        r.setRequestedAt(Instant.now());
        reservationRepo.saveAndFlush(r); // unique index DB sẽ chặn nếu hai yêu cầu trùng lọt qua bước kiểm tra
        recordEvent(r.getId(), null, ReservationStatus.PENDING, "Tiếp nhận yêu cầu đặt mượn", "USER", user.getId());

        // Outbox: cùng transaction => không bao giờ có reservation mà thiếu message (hoặc ngược lại).
        UUID eventId = UUID.randomUUID();
        var event = new ReservationRequestedEvent(eventId, r.getId(), user.getId(), book.getId(),
                ReservationRequestedEvent.TYPE, Instant.now());
        outboxRepo.save(new OutboxMessage(eventId, r.getId(), event.eventType(), toJson(event)));
        return new ReservationCreatedResponse(r.getId(), r.getRequestId(), r.getStatus());
    }

    @Transactional(readOnly = true)
    public ReservationResponse get(UUID id, String requesterEmail, boolean staff) {
        Reservation reservation = reservationRepo.findWithDetailsById(id)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy yêu cầu đặt mượn"));
        assertOwnerOrStaff(reservation, requesterEmail, staff);
        return toResponse(reservation);
    }

    @Transactional(readOnly = true)
    public List<ReservationEventResponse> events(UUID id, String requesterEmail, boolean staff) {
        Reservation reservation = reservationRepo.findWithDetailsById(id)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy yêu cầu đặt mượn"));
        assertOwnerOrStaff(reservation, requesterEmail, staff);
        return eventRepo.findByReservationIdOrderByCreatedAtAsc(id).stream()
                .map(e -> new ReservationEventResponse(e.getId(), e.getOldStatus(), e.getNewStatus(), e.getNote(),
                        e.getActorType(), e.getCreatedAt())).toList();
    }

    @Transactional(readOnly = true)
    public PageResponse<ReservationResponse> listByUser(UUID userId, ReservationStatus status, int page, int size,
                                                                  String requesterEmail, boolean staff) {
        User requester = userRepo.findByEmailIgnoreCase(requesterEmail)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng"));
        if (!staff && !requester.getId().equals(userId)) {
            throw ApiException.forbidden("Bạn không có quyền xem yêu cầu của người dùng khác.");
        }
        if (!userRepo.existsById(userId)) throw ApiException.notFound("Không tìm thấy người dùng");
        Pageable p = pageable(page, size);
        Page<Reservation> result = status == null ? reservationRepo.findByUserId(userId, p)
                : reservationRepo.findByUserIdAndStatus(userId, status, p);
        return PageResponse.of(result, this::toResponse);
    }

    @Transactional(readOnly = true)
    public PageResponse<ReservationResponse> listAll(ReservationStatus status, int page, int size) {
        Pageable p = pageable(page, size);
        Page<Reservation> result = status == null ? reservationRepo.findAll(p) : reservationRepo.findByStatus(status, p);
        return PageResponse.of(result, this::toResponse);
    }

    /** Hủy yêu cầu PENDING/RESERVED. Nếu đang giữ bản sao thì trả bản sao về AVAILABLE. */
    @Transactional
    public ReservationResponse cancel(UUID id, String requesterEmail, boolean staff) {
        Reservation r = lock(id);
        assertOwnerOrStaff(r, requesterEmail, staff);
        ReservationStatus from = r.getStatus();
        checkTransition(from, ReservationStatus.CANCELLED);
        if (from == ReservationStatus.RESERVED && r.getBookCopy() != null) {
            BookCopy copy = copyRepo.findByIdForUpdate(r.getBookCopy().getId()).orElseThrow();
            copy.setStatus(CopyStatus.AVAILABLE);
        }
        r.setStatus(ReservationStatus.CANCELLED);
        User actor = userRepo.findByEmailIgnoreCase(requesterEmail)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng"));
        recordEvent(id, from, ReservationStatus.CANCELLED, "Hủy yêu cầu",
                staff ? "STAFF" : "USER", actor.getId());
        return toResponse(r);
    }

    /** Xác nhận giao sách: chỉ cho phép khi reservation đang RESERVED. */
    @Transactional
    public ReservationResponse borrow(UUID id) {
        Reservation r = lock(id);
        checkTransition(r.getStatus(), ReservationStatus.BORROWED);
        BookCopy copy = copyRepo.findByIdForUpdate(r.getBookCopy().getId()).orElseThrow();
        Instant now = Instant.now();
        copy.setStatus(CopyStatus.BORROWED);
        r.setStatus(ReservationStatus.BORROWED);
        r.setBorrowedAt(now);
        r.setDueDate(now.plus(Duration.ofDays(loanDays)));
        recordEvent(id, ReservationStatus.RESERVED, ReservationStatus.BORROWED, "Giao sách cho người dùng", "STAFF", null);
        return toResponse(r);
    }

    /** Xác nhận trả sách: chỉ cho phép khi đang BORROWED; bản sao về AVAILABLE. */
    @Transactional
    public ReservationResponse giveBack(UUID id) {
        Reservation r = lock(id);
        checkTransition(r.getStatus(), ReservationStatus.RETURNED);
        BookCopy copy = copyRepo.findByIdForUpdate(r.getBookCopy().getId()).orElseThrow();
        copy.setStatus(CopyStatus.AVAILABLE);
        r.setStatus(ReservationStatus.RETURNED);
        r.setReturnedAt(Instant.now());
        recordEvent(id, ReservationStatus.BORROWED, ReservationStatus.RETURNED, "Nhận trả sách", "STAFF", null);
        return toResponse(r);
    }

    private void assertOwnerOrStaff(Reservation reservation, String requesterEmail, boolean staff) {
        if (staff) return;

        User requester = userRepo.findByEmailIgnoreCase(requesterEmail)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng"));

        if (!reservation.getUser().getId().equals(requester.getId())) {
            throw ApiException.forbidden("Bạn không có quyền truy cập yêu cầu của người dùng khác.");
        }
    }
    private Reservation lock(UUID id) {
        return reservationRepo.findByIdForUpdate(id).orElseThrow(() -> ApiException.notFound("Không tìm thấy yêu cầu đặt mượn"));
    }

    private void checkTransition(ReservationStatus from, ReservationStatus to) {
        if (!from.canTransitionTo(to)) {
            throw ApiException.conflict("Không thể chuyển trạng thái từ " + from + " sang " + to);
        }
    }

    private void recordEvent(UUID reservationId, ReservationStatus from, ReservationStatus to, String note,
                             String actorType, UUID actorId) {
        ReservationEvent e = new ReservationEvent();
        e.setReservationId(reservationId);
        e.setOldStatus(from == null ? null : from.name());
        e.setNewStatus(to.name());
        e.setNote(note);
        e.setActorType(actorType);
        e.setActorId(actorId);
        eventRepo.save(e);
    }

    private String toJson(Object o) {
        try {
            return objectMapper.writeValueAsString(o);
        } catch (JsonProcessingException ex) {
            throw new IllegalStateException("Không thể tạo nội dung message", ex);
        }
    }

    private static Pageable pageable(int page, int size) {
        return PageRequest.of(Math.max(page, 0), Math.min(Math.max(size, 1), 200), Sort.by(Sort.Direction.DESC, "requestedAt"));
    }

    private ReservationResponse toResponse(Reservation r) {
        BookCopy c = r.getBookCopy();
        return new ReservationResponse(r.getId(), r.getRequestId(), r.getUser().getId(), r.getUser().getFullName(),
                r.getBook().getId(), r.getBook().getTitle(), r.getBook().getAuthor(),
                c == null ? null : c.getId(), c == null ? null : c.getCopyCode(), r.getStatus(),
                r.getRequestedAt(), r.getProcessedAt(), r.getBorrowedAt(), r.getDueDate(), r.getReturnedAt(),
                r.getRejectionReason(), r.getCreatedAt(), r.getUpdatedAt());
    }
}
