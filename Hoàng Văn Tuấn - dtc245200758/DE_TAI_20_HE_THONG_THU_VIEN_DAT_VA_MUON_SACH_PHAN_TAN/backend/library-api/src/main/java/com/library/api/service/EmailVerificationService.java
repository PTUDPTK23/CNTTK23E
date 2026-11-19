package com.library.api.service;

import com.library.api.dto.RegisterRequest;
import com.library.api.dto.UserResponse;
import com.library.api.entity.EmailVerificationCode;
import com.library.api.entity.User;
import com.library.api.enums.Role;
import com.library.api.enums.UserStatus;
import com.library.api.exception.ApiException;
import com.library.api.repository.EmailVerificationCodeRepository;
import com.library.api.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.security.SecureRandom;
import java.time.Duration;
import java.time.Instant;
import java.util.Locale;

@Service
@RequiredArgsConstructor
public class EmailVerificationService {

    private static final int MAX_ATTEMPTS = 5;
    private static final long OTP_MINUTES = 5;
    private static final long RESEND_SECONDS = 60;

    private final EmailVerificationCodeRepository codeRepository;
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JavaMailSender mailSender;

    private final SecureRandom secureRandom = new SecureRandom();

    @Value("${MAIL_USERNAME:}")
    private String senderEmail;

    @Transactional
    public String register(RegisterRequest request) {
        String email = normalize(request.email());

        if (userRepository.existsByEmailIgnoreCase(email)) {
            throw ApiException.conflict("Email này đã có tài khoản.");
        }

        EmailVerificationCode pending = codeRepository.findById(email).orElse(null);
        Instant now = Instant.now();

        if (pending != null && pending.getLastSentAt() != null
                && Duration.between(pending.getLastSentAt(), now).getSeconds() < RESEND_SECONDS) {
            throw ApiException.badRequest("Vui lòng chờ 60 giây trước khi gửi mã mới.");
        }

        EmailVerificationCode record = pending == null
                ? new EmailVerificationCode() : pending;

        String otp = generateOtp();

        record.setEmail(email);
        record.setFullName(request.fullName().trim());
        record.setPhone(request.phone());
        record.setPasswordHash(passwordEncoder.encode(request.password()));
        record.setCodeHash(passwordEncoder.encode(otp));
        record.setExpiresAt(now.plusSeconds(OTP_MINUTES * 60));
        record.setLastSentAt(now);
        record.setAttempts(0);

        codeRepository.save(record);
        sendOtp(email, otp);

        return "Đã gửi mã xác thực đến email của bạn. Mã có hiệu lực trong 5 phút.";
    }

    @Transactional(noRollbackFor = ApiException.class)
    public UserResponse verify(String rawEmail, String otp) {
        String email = normalize(rawEmail);

        EmailVerificationCode pending = codeRepository.findById(email)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy yêu cầu xác thực. Vui lòng đăng ký lại."));

        if (!pending.getExpiresAt().isAfter(Instant.now())) {
            codeRepository.delete(pending);
            throw ApiException.badRequest("Mã xác thực đã hết hạn. Vui lòng đăng ký lại.");
        }

        if (pending.getAttempts() >= MAX_ATTEMPTS) {
            codeRepository.delete(pending);
            throw ApiException.badRequest("Bạn đã nhập sai quá số lần cho phép. Vui lòng đăng ký lại.");
        }

        if (!passwordEncoder.matches(otp, pending.getCodeHash())) {
            pending.setAttempts(pending.getAttempts() + 1);
            codeRepository.saveAndFlush(pending);
            throw ApiException.badRequest("Mã xác thực không chính xác.");
        }

        if (userRepository.existsByEmailIgnoreCase(email)) {
            codeRepository.delete(pending);
            throw ApiException.conflict("Email này đã có tài khoản.");
        }

        User user = new User();
        user.setFullName(pending.getFullName());
        user.setEmail(email);
        user.setPhone(pending.getPhone());
        user.setPasswordHash(pending.getPasswordHash());
        user.setRole(Role.USER);
        user.setStatus(UserStatus.ACTIVE);

        User saved = userRepository.saveAndFlush(user);
        codeRepository.delete(pending);

        return new UserResponse(
                saved.getId(),
                saved.getFullName(),
                saved.getEmail(),
                saved.getPhone(),
                saved.getRole(),
                saved.getStatus(),
                saved.getCreatedAt()
        );
    }

    @Transactional
    public String resend(String rawEmail) {
        String email = normalize(rawEmail);

        EmailVerificationCode pending = codeRepository.findById(email)
                .orElseThrow(() -> ApiException.notFound("Không tìm thấy yêu cầu đăng ký cần xác thực."));

        if (!pending.getExpiresAt().isAfter(Instant.now())) {
            codeRepository.delete(pending);
            throw ApiException.badRequest("Yêu cầu đã hết hạn. Vui lòng đăng ký lại.");
        }

        if (pending.getLastSentAt() != null
                && Duration.between(pending.getLastSentAt(), Instant.now()).getSeconds() < RESEND_SECONDS) {
            throw ApiException.badRequest("Vui lòng chờ 60 giây trước khi gửi lại mã.");
        }

        String otp = generateOtp();
        pending.setCodeHash(passwordEncoder.encode(otp));
        pending.setExpiresAt(Instant.now().plusSeconds(OTP_MINUTES * 60));
        pending.setLastSentAt(Instant.now());
        pending.setAttempts(0);

        codeRepository.save(pending);
        sendOtp(email, otp);

        return "Đã gửi mã xác thực mới đến email của bạn.";
    }

    private String generateOtp() {
        return String.format(Locale.ROOT, "%06d", secureRandom.nextInt(1_000_000));
    }

    private String normalize(String email) {
        return email.trim().toLowerCase(Locale.ROOT);
    }

    private void sendOtp(String recipient, String otp) {
        if (senderEmail == null || senderEmail.isBlank()) {
            throw new IllegalStateException(
                    "Chưa cấu hình MAIL_USERNAME và MAIL_PASSWORD trong môi trường máy chủ."
            );
        }

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(senderEmail);
        message.setTo(recipient);
        message.setSubject("Mã xác thực tài khoản thư viện");
        message.setText(
                "Xin chào,\n\n"
                + "Mã xác thực đăng ký tài khoản thư viện của bạn là: " + otp + "\n\n"
                + "Mã có hiệu lực trong 5 phút và chỉ sử dụng được một lần.\n"
                + "Nếu bạn không thực hiện đăng ký, hãy bỏ qua email này.\n\n"
                + "Hệ thống quản lý thư viện."
        );

        mailSender.send(message);
    }
}
