package com.library.api.dto;

public record CopyCounts(long total, long available, long reserved, long borrowed) {
    public static final CopyCounts EMPTY = new CopyCounts(0, 0, 0, 0);
}
