package com.library.api.service;

import com.library.api.dto.PageResponse;
import com.library.api.dto.UserRequest;
import com.library.api.dto.UserResponse;
import com.library.api.enums.UserStatus;
import com.library.api.entity.User;
import com.library.api.enums.Role;
import com.library.api.exception.ApiException;
import com.library.api.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.UUID;

@Service
@RequiredArgsConstructor
public class UserService {
    private final UserRepository repo;

    @Transactional(readOnly = true)
    public PageResponse<UserResponse> list(int page, int size) {
        return PageResponse.of(repo.findAll(PageRequest.of(Math.max(page, 0), Math.min(Math.max(size, 1), 200), Sort.by("fullName"))),
                this::toResponse);
    }

    @Transactional(readOnly = true)
    public UserResponse get(UUID id) {
        return toResponse(repo.findById(id).orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng")));
    }

    @Transactional
    public UserResponse create(UserRequest req) {
        String email = req.email().trim().toLowerCase();
        if (repo.existsByEmailIgnoreCase(email)) throw ApiException.conflict("Email đã được sử dụng");
        User u = new User();
        u.setFullName(req.fullName().trim());
        u.setEmail(email);
        u.setPhone(req.phone());
        u.setRole(req.role() == null ? Role.USER : req.role());
        return toResponse(repo.save(u));
    }

    /** Khóa/mở khóa tài khoản. Tài khoản bị khóa không thể gửi yêu cầu đặt mượn mới. */
    @Transactional
    public UserResponse setStatus(UUID id, UserStatus status) {
        User u = repo.findById(id).orElseThrow(() -> ApiException.notFound("Không tìm thấy người dùng"));
        u.setStatus(status);
        return toResponse(repo.save(u));
    }

    private UserResponse toResponse(User u) {
        return new UserResponse(u.getId(), u.getFullName(), u.getEmail(), u.getPhone(), u.getRole(), u.getStatus(), u.getCreatedAt());
    }
}
