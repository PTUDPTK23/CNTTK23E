package com.notification.repository;

import com.notification.entity.Recipient;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Map;

@Repository
public interface RecipientRepository extends JpaRepository<Recipient, Long> {

    List<Recipient> findByStatus(String status);

    List<Recipient> findByRecipientGroup(String recipientGroup);

    List<Recipient> findByRecipientGroupAndStatus(String recipientGroup, String status);

    long countByStatus(String status);

    @Query("SELECT r.recipientGroup AS groupName, COUNT(r) AS count FROM Recipient r GROUP BY r.recipientGroup")
    List<Map<String, Object>> countRecipientsByGroup();

    @Query("SELECT r FROM Recipient r WHERE " +
           "(:keyword IS NULL OR LOWER(r.name) LIKE LOWER(CONCAT('%', :keyword, '%')) " +
           "OR LOWER(r.email) LIKE LOWER(CONCAT('%', :keyword, '%')) " +
           "OR LOWER(r.phone) LIKE LOWER(CONCAT('%', :keyword, '%'))) " +
           "AND (:group IS NULL OR :group = '' OR :group = 'ALL' OR r.recipientGroup = :group) " +
           "AND (:status IS NULL OR :status = '' OR :status = 'ALL' OR r.status = :status) " +
           "ORDER BY r.id DESC")
    List<Recipient> searchRecipients(@Param("keyword") String keyword,
                                     @Param("group") String group,
                                     @Param("status") String status);
}
