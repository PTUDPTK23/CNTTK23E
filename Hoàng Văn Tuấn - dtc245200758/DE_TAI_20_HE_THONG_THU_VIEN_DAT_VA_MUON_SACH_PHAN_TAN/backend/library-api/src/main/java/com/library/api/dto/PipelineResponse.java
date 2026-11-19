package com.library.api.dto;

/** Tình trạng đường ống bất đồng bộ: Outbox -> RabbitMQ -> Worker. */
public record PipelineResponse(boolean rabbitUp, long queueMessages, long consumers, long deadLetterMessages,
                               long outboxPending, long outboxPublished) {
}
