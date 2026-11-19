package com.library.worker.consumer;

import com.library.worker.config.RabbitConfig;
import com.library.worker.dto.ReservationRequestedEvent;
import com.library.worker.service.ReservationProcessor;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

/**
 * Chế độ ACK = AUTO: Spring chỉ ACK sau khi method này return (transaction đã commit).
 * Nếu ném exception: retry tối đa 3 lần (cấu hình application.yml), hết lần thì reject không requeue => DLQ.
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class ReservationConsumer {
    private final ReservationProcessor processor;

    @RabbitListener(queues = RabbitConfig.QUEUE)
    public void onMessage(ReservationRequestedEvent event) {
        log.info("Nhận event {} cho reservation {}", event.eventId(), event.reservationId());
        processor.process(event);
    }
}
