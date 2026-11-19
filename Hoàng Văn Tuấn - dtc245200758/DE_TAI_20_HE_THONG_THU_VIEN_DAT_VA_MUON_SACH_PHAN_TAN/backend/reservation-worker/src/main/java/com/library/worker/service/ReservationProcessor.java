package com.library.worker.service;

import com.library.worker.dto.ReservationRequestedEvent;
import com.library.worker.entity.BookCopy;
import com.library.worker.entity.Reservation;
import com.library.worker.enums.CopyStatus;
import com.library.worker.enums.ReservationStatus;
import com.library.worker.repository.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.Optional;

/**
 * Lõi nghiệp vụ của Worker. Toàn bộ chạy trong MỘT transaction:
 *  1) ghi nhận eventId (chống xử lý trùng)  2) khóa reservation  3) khóa và giữ một bản sao  4) cập nhật + ghi lịch sử.
 * Nếu có exception => rollback tất cả => message được retry (và cuối cùng vào DLQ), không để dữ liệu nửa vời.
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class ReservationProcessor {
    public enum Outcome { RESERVED, REJECTED, DUPLICATE, SKIPPED }

    private final ProcessedEventStore processedEvents;
    private final ReservationRepository reservationRepo;
    private final BookCopyRepository copyRepo;
    private final UserRefRepository userRepo;
    private final BookRefRepository bookRepo;
    private final ReservationHistoryWriter history;

    @Transactional
    public Outcome process(ReservationRequestedEvent ev) {
        // Bước 1-2 (spec): đọc eventId, kiểm tra đã xử lý chưa.
        if (!processedEvents.markProcessed(ev.eventId(), ev.reservationId())) {
            log.info("Bỏ qua event trùng {}", ev.eventId());
            return Outcome.DUPLICATE;
        }

        // Khóa dòng reservation: tuần tự hóa với thao tác hủy từ API.
        Reservation r = reservationRepo.findByIdForUpdate(ev.reservationId())
                .orElseThrow(() -> new IllegalStateException("Không tìm thấy reservation " + ev.reservationId()));

        // Chỉ xử lý yêu cầu còn PENDING (có thể đã bị hủy hoặc đã xử lý).
        if (r.getStatus() != ReservationStatus.PENDING) {
            log.info("Reservation {} đang {} - bỏ qua", r.getId(), r.getStatus());
            return Outcome.SKIPPED;
        }

        // Lỗi nghiệp vụ/dữ liệu: cập nhật REJECTED, KHÔNG retry.
        if (!userRepo.existsByIdAndStatus(r.getUserId(), "ACTIVE")) {
            return reject(r, "Người dùng không tồn tại hoặc đã bị khóa");
        }
        if (!bookRepo.existsByIdAndActiveTrue(r.getBookId())) {
            return reject(r, "Đầu sách không còn được phục vụ");
        }

        // Chọn + khóa bản sao trong cùng transaction (FOR UPDATE SKIP LOCKED) => không có race condition.
        Optional<BookCopy> copy = copyRepo.lockFirstAvailable(r.getBookId());
        if (copy.isEmpty()) {
            return reject(r, "Hết bản sách khả dụng");
        }

        Instant now = Instant.now();
        BookCopy c = copy.get();
        c.setStatus(CopyStatus.RESERVED);
        c.setUpdatedAt(now);
        r.setBookCopyId(c.getId());
        r.setStatus(ReservationStatus.RESERVED);
        r.setProcessedAt(now);
        r.setUpdatedAt(now);
        history.record(r.getId(), ReservationStatus.PENDING.name(), ReservationStatus.RESERVED.name(),
                "Đã giữ bản sao " + c.getCopyCode());
        log.info("Reservation {} -> RESERVED (bản sao {})", r.getId(), c.getCopyCode());
        return Outcome.RESERVED;
    }

    private Outcome reject(Reservation r, String reason) {
        Instant now = Instant.now();
        r.setStatus(ReservationStatus.REJECTED);
        r.setRejectionReason(reason);
        r.setProcessedAt(now);
        r.setUpdatedAt(now);
        history.record(r.getId(), ReservationStatus.PENDING.name(), ReservationStatus.REJECTED.name(), reason);
        log.info("Reservation {} -> REJECTED ({})", r.getId(), reason);
        return Outcome.REJECTED;
    }
}
