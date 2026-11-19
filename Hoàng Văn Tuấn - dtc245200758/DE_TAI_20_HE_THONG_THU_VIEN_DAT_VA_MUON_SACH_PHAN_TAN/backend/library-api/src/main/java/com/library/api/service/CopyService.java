package com.library.api.service;

import com.library.api.dto.CopyRequest;
import com.library.api.dto.CopyResponse;
import com.library.api.entity.Book;
import com.library.api.entity.BookCopy;
import com.library.api.enums.CopyStatus;
import com.library.api.exception.ApiException;
import com.library.api.repository.BookCopyRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Set;
import java.util.UUID;

@Service
@RequiredArgsConstructor
public class CopyService {
    private final BookCopyRepository copyRepo;
    private final BookService bookService;

    @Transactional(readOnly = true)
    public List<CopyResponse> list(UUID bookId) {
        bookService.findActive(bookId);
        return copyRepo.findByBookIdOrderByCopyCode(bookId).stream().map(this::toResponse).toList();
    }

    @Transactional
    public CopyResponse add(UUID bookId, CopyRequest req) {
        Book book = bookService.findActive(bookId);
        String code = req == null || req.copyCode() == null || req.copyCode().isBlank()
                ? generateCode(book) : req.copyCode().trim();
        if (copyRepo.existsByCopyCode(code)) throw ApiException.conflict("Mã bản sao đã tồn tại");
        BookCopy c = new BookCopy();
        c.setBook(book);
        c.setCopyCode(code);
        c.setStatus(CopyStatus.AVAILABLE);
        return toResponse(copyRepo.save(c));
    }

    /**
     * Đổi trạng thái thủ công. Bản sao RESERVED/BORROWED do vòng đời reservation quản lý,
     * không cho đổi tay (tránh biến bản đang mượn thành AVAILABLE).
     */
    @Transactional
    public CopyResponse updateStatus(UUID copyId, CopyStatus target) {
        // Khóa dòng để Worker (SKIP LOCKED) không giữ bản sao này trong lúc đang đổi trạng thái.
        BookCopy c = copyRepo.findByIdForUpdate(copyId).orElseThrow(() -> ApiException.notFound("Không tìm thấy bản sao"));
        if (c.getStatus() == target) return toResponse(c);
        if (c.getStatus() == CopyStatus.RESERVED || c.getStatus() == CopyStatus.BORROWED) {
            throw ApiException.conflict("Bản sao đang " + (c.getStatus() == CopyStatus.RESERVED ? "được giữ" : "được mượn")
                    + ". Hãy xử lý qua yêu cầu đặt mượn tương ứng.");
        }
        Set<CopyStatus> manual = Set.of(CopyStatus.AVAILABLE, CopyStatus.MAINTENANCE, CopyStatus.LOST);
        if (!manual.contains(target)) {
            throw ApiException.conflict("Không thể đặt trạng thái " + target + " thủ công");
        }
        c.setStatus(target);
        return toResponse(copyRepo.save(c));
    }

    private String generateCode(Book book) {
        String prefix = "BK-" + book.getId().toString().substring(0, 8).toUpperCase();
        long n = copyRepo.countByBookId(book.getId()) + 1;
        String code;
        do {
            code = prefix + "-" + String.format("%02d", n++);
        } while (copyRepo.existsByCopyCode(code));
        return code;
    }

    private CopyResponse toResponse(BookCopy c) {
        return new CopyResponse(c.getId(), c.getBook().getId(), c.getCopyCode(), c.getStatus(), c.getCreatedAt(), c.getUpdatedAt());
    }
}
