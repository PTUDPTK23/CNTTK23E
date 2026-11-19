package com.library.api.service;

import com.library.api.entity.BookCopy;
import com.library.api.entity.Reservation;
import com.library.api.entity.ReservationEvent;
import com.library.api.enums.CopyStatus;
import com.library.api.enums.ReservationStatus;
import com.library.api.repository.BookCopyRepository;
import com.library.api.repository.ReservationEventRepository;
import com.library.api.repository.ReservationRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.transaction.support.TransactionTemplate;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Tự động hủy các yêu cầu RESERVED mà người dùng không đến nhận sách trong thời hạn giữ (mặc định 48 giờ),
 * trả bản sao về AVAILABLE để người khác có thể đặt. Mỗi yêu cầu xử lý trong transaction riêng, có khóa dòng.
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class ReservationExpiryService {
    private final ReservationRepository reservationRepo;
    private final BookCopyRepository copyRepo;
    private final ReservationEventRepository eventRepo;
    private final TransactionTemplate tx;

    @Value("${app.reservation.hold-hours:48}")
    private long holdHours;

    @Scheduled(fixedDelayString = "${app.reservation.expiry-interval-ms:60000}", initialDelayString = "${app.reservation.expiry-interval-ms:60000}")
    public void expireStaleHolds() {
        try {
            int n = expireOlderThan(Instant.now().minus(Duration.ofHours(holdHours)));
            if (n > 0) log.info("Đã tự động hủy {} yêu cầu giữ sách quá hạn", n);
        } catch (Exception e) {
            log.warn("Tác vụ hủy giữ sách quá hạn lỗi, sẽ thử lại: {}", e.getMessage());
        }
    }

    public int expireOlderThan(Instant cutoff) {
        List<UUID> ids = reservationRepo.findStaleIds(ReservationStatus.RESERVED, cutoff);
        int done = 0;
        for (UUID id : ids) {
            Boolean ok = tx.execute(status -> expireOne(id));
            if (Boolean.TRUE.equals(ok)) done++;
        }
        return done;
    }

    private boolean expireOne(UUID id) {
        Reservation r = reservationRepo.findByIdForUpdate(id).orElse(null);
        if (r == null || r.getStatus() != ReservationStatus.RESERVED) return false; // đã đổi trạng thái (nhận sách/hủy)
        if (r.getBookCopy() != null) {
            BookCopy copy = copyRepo.findByIdForUpdate(r.getBookCopy().getId()).orElseThrow();
            copy.setStatus(CopyStatus.AVAILABLE);
        }
        r.setStatus(ReservationStatus.CANCELLED);
        ReservationEvent e = new ReservationEvent();
        e.setReservationId(id);
        e.setOldStatus(ReservationStatus.RESERVED.name());
        e.setNewStatus(ReservationStatus.CANCELLED.name());
        e.setNote("Quá hạn nhận sách (" + holdHours + " giờ) - hệ thống tự động hủy");
        e.setActorType("SYSTEM");
        eventRepo.save(e);
        return true;
    }
}
