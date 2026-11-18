package com.notification.repository;

import com.notification.entity.CampaignRecipient;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface CampaignRecipientRepository extends JpaRepository<CampaignRecipient, Long> {
    List<CampaignRecipient> findByCampaignId(Long campaignId);
    void deleteByCampaignId(Long campaignId);
    long countByCampaignIdAndStatus(Long campaignId, String status);
}
