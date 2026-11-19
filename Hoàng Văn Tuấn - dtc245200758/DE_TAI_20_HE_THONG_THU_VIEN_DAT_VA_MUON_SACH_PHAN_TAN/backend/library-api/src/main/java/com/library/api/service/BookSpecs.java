package com.library.api.service;

import com.library.api.entity.Book;
import com.library.api.entity.BookCopy;
import com.library.api.enums.CopyStatus;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Subquery;
import org.springframework.data.jpa.domain.Specification;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

public final class BookSpecs {
    private BookSpecs() {
    }

    /** availability: "AVAILABLE" (còn bản), "UNAVAILABLE" (hết bản), null = tất cả. */
    public static Specification<Book> filter(String keyword, UUID categoryId, String author, String availability) {
        return (root, query, cb) -> {
            List<Predicate> ps = new ArrayList<>();
            ps.add(cb.isTrue(root.get("active")));
            if (keyword != null && !keyword.isBlank()) {
                String like = "%" + keyword.trim().toLowerCase() + "%";
                ps.add(cb.or(
                        cb.like(cb.lower(root.get("title")), like),
                        cb.like(cb.lower(root.get("author")), like),
                        cb.like(cb.lower(cb.coalesce(root.<String>get("isbn"), "")), like)));
            }
            if (categoryId != null) {
                ps.add(cb.equal(root.get("category").get("id"), categoryId));
            }
            if (author != null && !author.isBlank()) {
                ps.add(cb.like(cb.lower(root.get("author")), "%" + author.trim().toLowerCase() + "%"));
            }
            if (availability != null && !availability.isBlank()) {
                Predicate hasAvailable = cb.exists(availableCopySubquery(root, query.subquery(Long.class), cb));
                if ("AVAILABLE".equalsIgnoreCase(availability)) ps.add(hasAvailable);
                else if ("UNAVAILABLE".equalsIgnoreCase(availability)) ps.add(cb.not(hasAvailable));
            }
            return cb.and(ps.toArray(new Predicate[0]));
        };
    }

    private static Subquery<Long> availableCopySubquery(Root<Book> root, Subquery<Long> sq,
                                                        jakarta.persistence.criteria.CriteriaBuilder cb) {
        Root<BookCopy> c = sq.from(BookCopy.class);
        sq.select(cb.literal(1L)).where(cb.equal(c.get("book"), root), cb.equal(c.get("status"), CopyStatus.AVAILABLE));
        return sq;
    }
}
