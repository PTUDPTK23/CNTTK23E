package com.notification.repository;

import com.notification.entity.NotificationLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;

@Repository
public interface NotificationLogRepository extends JpaRepository<NotificationLog, Long> {

    List<NotificationLog> findAllByOrderBySentAtDesc();

    List<NotificationLog> findTop50ByOrderBySentAtDesc();

    List<NotificationLog> findByCampaignIdOrderBySentAtDesc(Long campaignId);

    long countByStatus(String status);

    @Query("SELECT l FROM NotificationLog l WHERE " +
           "(:keyword IS NULL OR :keyword = '' OR LOWER(l.recipientEmail) LIKE LOWER(CONCAT('%', :keyword, '%')) OR LOWER(l.recipientName) LIKE LOWER(CONCAT('%', :keyword, '%'))) " +
           "AND (:status IS NULL OR :status = '' OR :status = 'ALL' OR l.status = :status) " +
           "AND (:channel IS NULL OR :channel = '' OR :channel = 'ALL' OR l.channel = :channel) " +
           "AND (:startDate IS NULL OR l.sentAt >= :startDate) " +
           "AND (:endDate IS NULL OR l.sentAt <= :endDate) " +
           "ORDER BY l.sentAt DESC")
    List<NotificationLog> searchLogs(@Param("keyword") String keyword,
                                     @Param("status") String status,
                                     @Param("channel") String channel,
                                     @Param("startDate") LocalDateTime startDate,
                                     @Param("endDate") LocalDateTime endDate);

    @Query("SELECT FUNCTION('DATE_FORMAT', l.sentAt, '%Y-%m-%d') AS logDate, " +
           "l.status AS status, COUNT(l) AS count " +
           "FROM NotificationLog l " +
           "WHERE l.sentAt >= :sinceDate " +
           "GROUP BY FUNCTION('DATE_FORMAT', l.sentAt, '%Y-%m-%d'), l.status " +
           "ORDER BY logDate ASC")
    List<Map<String, Object>> countLogsByDayAndStatus(@Param("sinceDate") LocalDateTime sinceDate);
}
