package com.notification.dto.request;

import java.util.List;

public class SendBulkRequest {

    private Long notificationId;
    private String title;
    private String content;
    private String channel = "EMAIL";
    private String targetGroup; // ALL, KHACH_HANG, VIP, etc.
    private List<Long> recipientIds;
    private Boolean simulationMode; // Tùy chọn bật/tắt chế độ mô phỏng

    public SendBulkRequest() {}

    public Long getNotificationId() {
        return notificationId;
    }

    public void setNotificationId(Long notificationId) {
        this.notificationId = notificationId;
    }

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public String getContent() {
        return content;
    }

    public void setContent(String content) {
        this.content = content;
    }

    public String getChannel() {
        return channel;
    }

    public void setChannel(String channel) {
        this.channel = channel;
    }

    public String getTargetGroup() {
        return targetGroup;
    }

    public void setTargetGroup(String targetGroup) {
        this.targetGroup = targetGroup;
    }

    public List<Long> getRecipientIds() {
        return recipientIds;
    }

    public void setRecipientIds(List<Long> recipientIds) {
        this.recipientIds = recipientIds;
    }

    public Boolean getSimulationMode() {
        return simulationMode;
    }

    public void setSimulationMode(Boolean simulationMode) {
        this.simulationMode = simulationMode;
    }
}
