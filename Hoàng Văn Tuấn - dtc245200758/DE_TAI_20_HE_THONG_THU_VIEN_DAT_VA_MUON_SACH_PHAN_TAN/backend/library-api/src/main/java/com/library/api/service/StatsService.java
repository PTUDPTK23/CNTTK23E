package com.library.api.service;

import com.library.api.dto.CategoryStatResponse;
import com.library.api.dto.PipelineResponse;
import com.library.api.dto.StatsResponse;
import com.library.api.entity.OutboxMessage;
import com.library.api.enums.CopyStatus;
import com.library.api.enums.ReservationStatus;
import com.library.api.repository.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.core.AmqpAdmin;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.*;

@Service
@RequiredArgsConstructor
@Slf4j
public class StatsService {
    private final BookRepository bookRepo;
    private final BookCopyRepository copyRepo;
    private final ReservationRepository reservationRepo;
    private final UserRepository userRepo;
    private final OutboxRepository outboxRepo;
    private final AmqpAdmin amqpAdmin;

    @Transactional(readOnly = true)
    public StatsResponse stats() {
        long total = 0, available = 0, reserved = 0, borrowed = 0;
        for (Object[] row : copyRepo.countAllByStatus()) {
            long n = (Long) row[1];
            total += n;
            if (row[0] == CopyStatus.AVAILABLE) available += n;
            else if (row[0] == CopyStatus.RESERVED) reserved += n;
            else if (row[0] == CopyStatus.BORROWED) borrowed += n;
        }
        Map<String, Long> byStatus = new LinkedHashMap<>();
        for (ReservationStatus s : ReservationStatus.values()) byStatus.put(s.name(), 0L);
        for (Object[] row : reservationRepo.countGroupedByStatus()) byStatus.put(((ReservationStatus) row[0]).name(), (Long) row[1]);
        long overdue = reservationRepo.countByStatusAndDueDateBefore(ReservationStatus.BORROWED, Instant.now());
        return new StatsResponse(bookRepo.countByActiveTrue(), total, available, reserved, borrowed,
                reservationRepo.countByStatus(ReservationStatus.PENDING), userRepo.count(), overdue, byStatus);
    }

    @Transactional(readOnly = true)
    public List<CategoryStatResponse> byCategory() {
        Map<String, long[]> acc = new TreeMap<>();
        for (Object[] row : copyRepo.countByCategoryAndStatus()) {
            long[] a = acc.computeIfAbsent((String) row[0], k -> new long[2]);
            long n = (Long) row[2];
            a[0] += n;
            if (row[1] == CopyStatus.AVAILABLE) a[1] += n;
        }
        List<CategoryStatResponse> out = new ArrayList<>();
        acc.forEach((k, a) -> out.add(new CategoryStatResponse(k, a[0], a[1])));
        return out;
    }

    /** Tình trạng RabbitMQ/Outbox. Không ném lỗi khi RabbitMQ chết: trả rabbitUp=false để giao diện hiển thị. */
    @Transactional(readOnly = true)
    public PipelineResponse pipeline() {
        long pending = outboxRepo.countByStatus(OutboxMessage.PENDING);
        long published = outboxRepo.countByStatus(OutboxMessage.PUBLISHED);
        try {
            Properties q = amqpAdmin.getQueueProperties(com.library.api.config.RabbitConfig.QUEUE);
            Properties dlq = amqpAdmin.getQueueProperties(com.library.api.config.RabbitConfig.DLQ);
            if (q == null) return new PipelineResponse(true, 0, 0, 0, pending, published);
            return new PipelineResponse(true, num(q, "QUEUE_MESSAGE_COUNT"), num(q, "QUEUE_CONSUMER_COUNT"),
                    dlq == null ? 0 : num(dlq, "QUEUE_MESSAGE_COUNT"), pending, published);
        } catch (Exception e) {
            log.warn("Không đọc được trạng thái RabbitMQ: {}", e.getMessage());
            return new PipelineResponse(false, 0, 0, 0, pending, published);
        }
    }

    private static long num(Properties p, String key) {
        Object v = p.get(key);
        return v instanceof Number n ? n.longValue() : 0;
    }
}
