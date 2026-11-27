package com.library.api.service;

import com.library.api.dto.CategoryRequest;
import com.library.api.dto.CategoryResponse;
import com.library.api.entity.Category;
import com.library.api.exception.ApiException;
import com.library.api.repository.CategoryRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.UUID;

@Service
@RequiredArgsConstructor
public class CategoryService {
    private final CategoryRepository repo;

    @Transactional(readOnly = true)
    public List<CategoryResponse> list() {
        return repo.findAll(Sort.by("name")).stream().map(this::toResponse).toList();
    }

    @Transactional
    public CategoryResponse create(CategoryRequest req) {
        if (repo.existsByNameIgnoreCase(req.name().trim())) throw ApiException.conflict("Thể loại đã tồn tại");
        Category c = new Category();
        c.setName(req.name().trim());
        c.setDescription(req.description());
        return toResponse(repo.save(c));
    }

    @Transactional
    public CategoryResponse update(UUID id, CategoryRequest req) {
        Category c = repo.findById(id).orElseThrow(() -> ApiException.notFound("Không tìm thấy thể loại"));
        c.setName(req.name().trim());
        c.setDescription(req.description());
        return toResponse(repo.save(c));
    }

    private CategoryResponse toResponse(Category c) {
        return new CategoryResponse(c.getId(), c.getName(), c.getDescription());
    }
}
