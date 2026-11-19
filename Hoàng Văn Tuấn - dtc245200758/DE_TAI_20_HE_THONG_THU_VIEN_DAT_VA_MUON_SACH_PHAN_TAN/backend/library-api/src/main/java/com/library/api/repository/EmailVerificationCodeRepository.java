package com.library.api.repository;

import com.library.api.entity.EmailVerificationCode;
import org.springframework.data.jpa.repository.JpaRepository;

import java.time.Instant;

public interface EmailVerificationCodeRepository
        extends JpaRepository<EmailVerificationCode, String> {

    long deleteByExpiresAtBefore(Instant time);
}