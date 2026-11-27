package com.library.api.metrics;

import com.library.api.entity.OutboxMessage;
import com.library.api.enums.ReservationStatus;
import com.library.api.repository.OutboxRepository;
import com.library.api.repository.ReservationRepository;
import io.micrometer.core.instrument.Gauge;
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.stereotype.Component;

/**
 * Metric nghiệp vụ cho Prometheus/Grafana. Giá trị được đọc trực tiếp từ PostgreSQL mỗi lần Prometheus scrape,
 * nên luôn là số liệu thật (không cache, không dữ liệu giả).
 */
@Component
public class LibraryMetrics {
    public LibraryMetrics(MeterRegistry registry, OutboxRepository outboxRepo, ReservationRepository reservationRepo) {
        Gauge.builder("library_outbox_pending", outboxRepo, r -> r.countByStatus(OutboxMessage.PENDING))
                .description("So event Outbox dang cho gui sang RabbitMQ")
                .register(registry);
        Gauge.builder("library_outbox_published", outboxRepo, r -> r.countByStatus(OutboxMessage.PUBLISHED))
                .description("So event Outbox da gui thanh cong")
                .register(registry);
        for (ReservationStatus s : ReservationStatus.values()) {
            Gauge.builder("library_reservations", reservationRepo, r -> r.countByStatus(s))
                    .tag("status", s.name())
                    .description("So yeu cau dat muon theo trang thai")
                    .register(registry);
        }
    }
}