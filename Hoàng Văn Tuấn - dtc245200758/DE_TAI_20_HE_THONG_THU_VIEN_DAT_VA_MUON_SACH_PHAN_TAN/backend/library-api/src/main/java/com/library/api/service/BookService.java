package com.library.api.service;

import com.library.api.dto.*;
import com.library.api.entity.Book;
import com.library.api.entity.Category;
import com.library.api.enums.CopyStatus;
import com.library.api.enums.ReservationStatus;
import com.library.api.exception.ApiException;
import com.library.api.repository.BookCopyRepository;
import com.library.api.repository.BookRepository;
import com.library.api.repository.CategoryRepository;
import com.library.api.repository.ReservationRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.*;

@Service
@RequiredArgsConstructor
public class BookService {
    private final BookRepository bookRepo;
    private final CategoryRepository categoryRepo;
    private final BookCopyRepository copyRepo;
    private final ReservationRepository reservationRepo;

    @Transactional(readOnly = true)
    public PageResponse<BookResponse> search(String keyword, UUID categoryId, String author, String availability,
                                             int page, int size) {
        Pageable pageable = PageRequest.of(Math.max(page, 0), Math.min(Math.max(size, 1), 50), Sort.by("title"));
        Page<Book> result = bookRepo.findAll(BookSpecs.filter(keyword, categoryId, author, availability), pageable);
        Map<UUID, CopyCounts> counts = countsFor(result.getContent().stream().map(Book::getId).toList());
        return PageResponse.of(result, b -> toResponse(b, counts.getOrDefault(b.getId(), CopyCounts.EMPTY)));
    }

    @Transactional(readOnly = true)
    public BookResponse get(UUID id) {
        Book b = findActive(id);
        return toResponse(b, countsFor(List.of(id)).getOrDefault(id, CopyCounts.EMPTY));
    }

    @Transactional
    public BookResponse create(BookRequest req) {
        String isbn = normalize(req.isbn());
        if (isbn != null && bookRepo.existsByIsbn(isbn)) throw ApiException.conflict("ISBN đã tồn tại");
        Book b = new Book();
        apply(b, req, isbn);
        Book saved = bookRepo.save(b);
        return toResponse(saved, CopyCounts.EMPTY);
    }

    @Transactional
    public BookResponse update(UUID id, BookRequest req) {
        Book b = findActive(id);
        String isbn = normalize(req.isbn());
        if (isbn != null && bookRepo.existsByIsbnAndIdNot(isbn, id)) throw ApiException.conflict("ISBN đã tồn tại");
        apply(b, req, isbn);
        Book saved = bookRepo.save(b);
        return toResponse(saved, countsFor(List.of(id)).getOrDefault(id, CopyCounts.EMPTY));
    }

    /** Xóa mềm (active=false) để không mất lịch sử đặt mượn; từ chối nếu còn yêu cầu đang hoạt động. */
    @Transactional
    public void delete(UUID id) {
        Book b = findActive(id);
        if (reservationRepo.existsByBookIdAndStatusIn(id, ReservationStatus.ACTIVE)) {
            throw ApiException.conflict("Không thể xóa: sách còn yêu cầu đang chờ, đã giữ hoặc đang mượn");
        }
        b.setActive(false);
        bookRepo.save(b);
    }

    @Transactional(readOnly = true)
    public AvailabilityResponse availability(UUID id) {
        findActive(id);
        CopyCounts c = countsFor(List.of(id)).getOrDefault(id, CopyCounts.EMPTY);
        long other = c.total() - c.available() - c.reserved() - c.borrowed();
        return new AvailabilityResponse(id, c.total(), c.available(), c.reserved(), c.borrowed(), other, c.available() > 0);
    }

    Book findActive(UUID id) {
        return bookRepo.findByIdAndActiveTrue(id).orElseThrow(() -> ApiException.notFound("Không tìm thấy sách"));
    }

    private void apply(Book b, BookRequest req, String isbn) {
        Category category = categoryRepo.findById(req.categoryId())
                .orElseThrow(() -> ApiException.badRequest("Thể loại không tồn tại"));
        b.setTitle(req.title().trim());
        b.setAuthor(req.author().trim());
        b.setIsbn(isbn);
        b.setPublisher(normalize(req.publisher()));
        b.setPublicationYear(req.publicationYear());
        b.setCategory(category);
        b.setDescription(normalize(req.description()));
        String coverImage = normalize(req.coverImage());
        if (coverImage != null) {
            b.setCoverImage(coverImage);
        }
    }

    private static String normalize(String s) {
        return s == null || s.isBlank() ? null : s.trim();
    }

    private Map<UUID, CopyCounts> countsFor(Collection<UUID> ids) {
        if (ids.isEmpty()) return Map.of();
        Map<UUID, long[]> acc = new HashMap<>(); // [total, available, reserved, borrowed]
        for (Object[] row : copyRepo.countByBooks(ids)) {
            long[] a = acc.computeIfAbsent((UUID) row[0], k -> new long[4]);
            long n = (Long) row[2];
            a[0] += n;
            CopyStatus st = (CopyStatus) row[1];
            if (st == CopyStatus.AVAILABLE) a[1] += n;
            else if (st == CopyStatus.RESERVED) a[2] += n;
            else if (st == CopyStatus.BORROWED) a[3] += n;
        }
        Map<UUID, CopyCounts> out = new HashMap<>();
        acc.forEach((k, a) -> out.put(k, new CopyCounts(a[0], a[1], a[2], a[3])));
        return out;
    }

    private BookResponse toResponse(Book b, CopyCounts c) {
        return new BookResponse(b.getId(), b.getIsbn(), b.getTitle(), b.getAuthor(), b.getPublisher(),
                b.getPublicationYear(), b.getCategory().getId(), b.getCategory().getName(), b.getDescription(),
                b.getCoverImage(), c.total(), c.available(), c.reserved(), c.borrowed(), b.getCreatedAt());
    }
}
