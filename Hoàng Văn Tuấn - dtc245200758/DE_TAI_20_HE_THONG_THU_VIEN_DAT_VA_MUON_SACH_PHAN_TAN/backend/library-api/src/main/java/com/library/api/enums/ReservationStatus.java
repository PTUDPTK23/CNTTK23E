package com.library.api.enums;

import java.util.EnumSet;
import java.util.Set;

public enum ReservationStatus {
    PENDING, RESERVED, REJECTED, BORROWED, RETURNED, CANCELLED;

    public static final Set<ReservationStatus> ACTIVE = EnumSet.of(PENDING, RESERVED, BORROWED);

    /** Quy tắc chuyển trạng thái hợp lệ (máy trạng thái). */
    public boolean canTransitionTo(ReservationStatus target) {
        return switch (this) {
            case PENDING -> target == RESERVED || target == REJECTED || target == CANCELLED;
            case RESERVED -> target == BORROWED || target == CANCELLED;
            case BORROWED -> target == RETURNED;
            case REJECTED, RETURNED, CANCELLED -> false; // trạng thái cuối
        };
    }
}
