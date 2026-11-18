package com.notification.repository;

import com.notification.entity.Campaign;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface CampaignRepository extends JpaRepository<Campaign, Long> {

    List<Campaign> findAllByOrderByCreatedAtDesc();

    List<Campaign> findByStatus(String status);

    @Query("SELECT c FROM Campaign c WHERE c.status = 'SCHEDULED' AND c.scheduledAt <= :currentTime")
    List<Campaign> findPendingScheduledCampaigns(@Param("currentTime") LocalDateTime currentTime);

    long countByStatus(String status);
}
