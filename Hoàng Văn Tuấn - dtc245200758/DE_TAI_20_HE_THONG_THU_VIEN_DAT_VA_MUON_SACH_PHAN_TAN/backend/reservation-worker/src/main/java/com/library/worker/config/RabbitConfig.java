package com.library.worker.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.amqp.core.*;
import org.springframework.amqp.support.converter.DefaultJackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Khai báo queue/exchange GIỐNG HỆT Library API (cùng tham số) để tránh PRECONDITION_FAILED. */
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

    /** JSON -> record, suy ra kiểu từ tham số listener (message do API gửi không mang type header của Worker). */
    @Bean
    MessageConverter jsonMessageConverter(ObjectMapper objectMapper) {
        Jackson2JsonMessageConverter converter = new Jackson2JsonMessageConverter(objectMapper);
        DefaultJackson2JavaTypeMapper typeMapper = new DefaultJackson2JavaTypeMapper();
        typeMapper.setTypePrecedence(Jackson2JavaTypeMapper.TypePrecedence.INFERRED);
        typeMapper.setTrustedPackages("*");
        converter.setJavaTypeMapper(typeMapper);
        return converter;
    }
}
