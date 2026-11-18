package com.notification.dto.request;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;

public class RecipientRequest {

    @NotBlank(message = "Tên người nhận không được để trống")
    private String name;

    @NotBlank(message = "Email người nhận không được để trống")
    @Email(message = "Định dạng email không hợp lệ")
    private String email;

    private String phone;

    @NotBlank(message = "Nhóm người nhận không được để trống")
    private String recipientGroup; // KHACH_HANG, NHAN_VIEN, DOI_TAC, VIP, SINH_VIEN

    private String status = "ACTIVE"; // ACTIVE, INACTIVE

    public RecipientRequest() {}

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getPhone() {
        return phone;
    }

    public void setPhone(String phone) {
        this.phone = phone;
    }

    public String getRecipientGroup() {
        return recipientGroup;
    }

    public void setRecipientGroup(String recipientGroup) {
        this.recipientGroup = recipientGroup;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }
}
