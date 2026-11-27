package com.library.api.dto;

import java.util.Map;

public record StatsResponse(long totalBooks, long totalCopies, long availableCopies, long reservedCopies,
                            long borrowedCopies, long pendingReservations, long totalUsers,
                            long overdueLoans, Map<String, Long> reservationsByStatus) {
}
