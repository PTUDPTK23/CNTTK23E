package com.library.api;

import com.library.api.enums.ReservationStatus;
import org.junit.jupiter.api.Test;

import static com.library.api.enums.ReservationStatus.*;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class ReservationStatusTest {
    @Test
    void validTransitions() {
        assertTrue(PENDING.canTransitionTo(RESERVED));
        assertTrue(PENDING.canTransitionTo(REJECTED));
        assertTrue(PENDING.canTransitionTo(CANCELLED));
        assertTrue(RESERVED.canTransitionTo(BORROWED));
        assertTrue(RESERVED.canTransitionTo(CANCELLED));
        assertTrue(BORROWED.canTransitionTo(RETURNED));
    }

    @Test
    void invalidTransitions() {
        assertFalse(PENDING.canTransitionTo(BORROWED));
        assertFalse(REJECTED.canTransitionTo(BORROWED));
        assertFalse(RETURNED.canTransitionTo(BORROWED));
        assertFalse(BORROWED.canTransitionTo(CANCELLED));
        assertFalse(RESERVED.canTransitionTo(RETURNED));
    }

    @Test
    void finalStatesHaveNoTransitions() {
        for (ReservationStatus from : new ReservationStatus[]{REJECTED, RETURNED, CANCELLED}) {
            for (ReservationStatus to : ReservationStatus.values()) {
                assertFalse(from.canTransitionTo(to), from + " -> " + to);
            }
        }
    }
}
