-- ==========================================================
-- BulkNotificationSystem - PostgreSQL Database Initialization
-- ==========================================================

CREATE TABLE IF NOT EXISTS "Campaigns" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(200) NOT NULL,
    "Title" VARCHAR(250) NOT NULL,
    "Content" TEXT NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Draft',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CompletedAt" TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS "IX_Campaigns_CreatedAt" ON "Campaigns" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Campaigns_Status" ON "Campaigns" ("Status");

CREATE TABLE IF NOT EXISTS "Recipients" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "FullName" VARCHAR(150) NOT NULL,
    "Email" VARCHAR(255) NULL,
    "PhoneNumber" VARCHAR(30) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "IX_Recipients_Email" ON "Recipients" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Recipients_PhoneNumber" ON "Recipients" ("PhoneNumber");

CREATE TABLE IF NOT EXISTS "NotificationDeliveries" (
    "JobId" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CampaignId" UUID NOT NULL REFERENCES "Campaigns" ("Id") ON DELETE CASCADE,
    "RecipientId" UUID NOT NULL REFERENCES "Recipients" ("Id") ON DELETE RESTRICT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "RetryCount" INT NOT NULL DEFAULT 0,
    "ErrorMessage" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ProcessedAt" TIMESTAMPTZ NULL,
    "PublishedAt" TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS "IX_NotificationDeliveries_Status" ON "NotificationDeliveries" ("Status");
CREATE INDEX IF NOT EXISTS "IX_NotificationDeliveries_Status_RetryCount" ON "NotificationDeliveries" ("Status", "RetryCount");
CREATE INDEX IF NOT EXISTS "IX_NotificationDeliveries_CampaignId" ON "NotificationDeliveries" ("CampaignId");
CREATE INDEX IF NOT EXISTS "IX_NotificationDeliveries_RecipientId" ON "NotificationDeliveries" ("RecipientId");

-- Seed người nhận mẫu
INSERT INTO "Recipients" ("Id", "FullName", "Email", "PhoneNumber", "CreatedAt")
VALUES 
    (gen_random_uuid(), 'Nguyễn Văn An', 'an.nguyen@example.com', '0901234567', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Trần Thị Bình', 'binh.tran@example.com', '0912345678', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Lê Hoàng Cường', 'cuong.le@example.com', '0987654321', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Phạm Thu Dung', 'dung.pham@example.com', '0934567890', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Hoàng Minh Đức', 'duc.hoang@example.com', '0945678901', CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;
