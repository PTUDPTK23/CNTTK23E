package com.library.api.service;

import com.library.api.config.RabbitConfig;
import com.library.api.entity.OutboxMessage;
import com.library.api.repository.OutboxRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageBuilder;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.transaction.support.TransactionTemplate;

import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.List;

/**
 * Tiến trình Outbox: định kỳ lấy các event chưa gửi, publish lên RabbitMQ, chờ broker xác nhận (publisher confirm)
 * rồi mới đánh dấu PUBLISHED. Nếu RabbitMQ chết, event vẫn nằm trong DB và được gửi lại ở lượt sau.
 * Đây là "at-least-once": Worker phải idempotent (đã xử lý bằng bảng processed_events).
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class OutboxPublisher {
    private final OutboxRepository outboxRepo;
    private final RabbitTemplate rabbitTemplate;
    private final TransactionTemplate tx;

    @Scheduled(fixedDelayString = "${app.outbox.poll-interval-ms:1000}")
    public void publishPending() {
        try {
            tx.executeWithoutResult(status -> relayBatch());
        } catch (Exception e) {
            log.warn("Outbox publisher lỗi lượt này, sẽ thử lại: {}", e.getMessage());
        }
    }

    private void relayBatch() {
        List<OutboxMessage> batch = outboxRepo.lockPending(50);
        for (OutboxMessage m : batch) {
            try {
                Message msg = MessageBuilder.withBody(m.getPayload().getBytes(StandardCharsets.UTF_8))
                        .setContentType(MessageProperties.CONTENT_TYPE_JSON)
                        .setMessageId(m.getId().toString())
                        .setDeliveryMode(MessageDeliveryMode.PERSISTENT)
                        .build();
                rabbitTemplate.invoke(ops -> {
                    ops.send(RabbitConfig.EXCHANGE, RabbitConfig.ROUTING_KEY, msg);
                    ops.waitForConfirmsOrDie(5000);
                    return true;
                });
                m.setStatus(OutboxMessage.PUBLISHED);
                m.setPublishedAt(Instant.now());
                log.info("Đã publish event {} (reservation {})", m.getId(), m.getAggregateId());
            } catch (Exception e) {
                m.setAttempts(m.getAttempts() + 1);
                String err = String.valueOf(e.getMessage());
                m.setLastError(err.length() > 480 ? err.substring(0, 480) : err);
                log.warn("Publish event {} thất bại (lần {}): {}", m.getId(), m.getAttempts(), err);
                break; // broker có thể đang lỗi: dừng lượt này, thử lại sau
            }
        }
    }
}
