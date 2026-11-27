package com.library.api.config;

import org.springframework.amqp.core.*;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Khai báo exchange/queue. Worker khai báo y hệt (cùng tham số) để không lỗi PRECONDITION_FAILED.
 * Luồng: exchange chính --(reservation.created)--> queue chính; message lỗi -> DLX -> DLQ.
 */
@Configuration
public class RabbitConfig {
    public static final String EXCHANGE = "library.reservation.exchange";
    public static final String ROUTING_KEY = "reservation.created";
    public static final String QUEUE = "reservation.request.queue";
    public static final String DLX = "library.reservation.dlx";
    public static final String DLQ_ROUTING_KEY = "reservation.request.dead";
    public static final String DLQ = "reservation.request.dlq";

    @Bean
    DirectExchange reservationExchange() {
        return new DirectExchange(EXCHANGE, true, false);
    }

    @Bean
    DirectExchange reservationDeadLetterExchange() {
        return new DirectExchange(DLX, true, false);
    }

    @Bean
    Queue reservationQueue() {
        return QueueBuilder.durable(QUEUE).deadLetterExchange(DLX).deadLetterRoutingKey(DLQ_ROUTING_KEY).build();
    }

    @Bean
    Queue reservationDeadLetterQueue() {
        return QueueBuilder.durable(DLQ).build();
    }

    @Bean
    Binding reservationBinding() {
        return BindingBuilder.bind(reservationQueue()).to(reservationExchange()).with(ROUTING_KEY);
    }

    @Bean
    Binding reservationDeadLetterBinding() {
        return BindingBuilder.bind(reservationDeadLetterQueue()).to(reservationDeadLetterExchange()).with(DLQ_ROUTING_KEY);
    }
}
