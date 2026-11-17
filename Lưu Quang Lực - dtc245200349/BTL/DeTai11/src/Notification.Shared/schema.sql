CREATE TABLE IF NOT EXISTS schema_versions (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS campaigns (
    id uuid PRIMARY KEY,
    name text NOT NULL,
    subject text NOT NULL,
    body text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    started_at timestamptz
);
CREATE TABLE IF NOT EXISTS deliveries (
    id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL REFERENCES campaigns(id),
    address text NOT NULL,
    status text NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Processing','RetryScheduled','Sent','Failed')),
    generation integer NOT NULL DEFAULT 1,
    attempt_count integer NOT NULL DEFAULT 0,
    total_attempts integer NOT NULL DEFAULT 0,
    next_attempt_at timestamptz,
    lease_until timestamptz,
    owner uuid,
    sent_at timestamptz,
    last_error text,
    UNIQUE(campaign_id, address)
);
CREATE INDEX IF NOT EXISTS ix_deliveries_campaign ON deliveries(campaign_id, status);
CREATE INDEX IF NOT EXISTS ix_deliveries_due ON deliveries(next_attempt_at) WHERE status = 'RetryScheduled';
CREATE INDEX IF NOT EXISTS ix_deliveries_lease ON deliveries(lease_until) WHERE status = 'Processing';
CREATE TABLE IF NOT EXISTS outbox (
    id uuid PRIMARY KEY,
    delivery_id uuid NOT NULL REFERENCES deliveries(id),
    campaign_id uuid NOT NULL REFERENCES campaigns(id),
    generation integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz,
    UNIQUE(delivery_id, generation)
);
CREATE INDEX IF NOT EXISTS ix_outbox_unpublished ON outbox(created_at) WHERE published_at IS NULL;
CREATE TABLE IF NOT EXISTS delivery_attempts (
    id bigserial PRIMARY KEY,
    delivery_id uuid NOT NULL REFERENCES deliveries(id),
    generation integer NOT NULL,
    worker_id text NOT NULL,
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    outcome text NOT NULL DEFAULT 'Processing',
    error text,
    UNIQUE(delivery_id, generation)
);
INSERT INTO schema_versions(version) VALUES (1) ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS audit_outbox (
    event_id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL,
    delivery_id uuid NOT NULL,
    generation integer NOT NULL,
    outcome text NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz,
    UNIQUE(delivery_id, generation)
);
CREATE INDEX IF NOT EXISTS ix_audit_outbox_unpublished ON audit_outbox(occurred_at) WHERE published_at IS NULL;
CREATE TABLE IF NOT EXISTS audit_events (
    event_id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL,
    delivery_id uuid NOT NULL,
    generation integer NOT NULL,
    outcome text NOT NULL,
    occurred_at timestamptz NOT NULL,
    recorded_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_audit_campaign ON audit_events(campaign_id, occurred_at DESC, event_id);
INSERT INTO schema_versions(version) VALUES (2) ON CONFLICT DO NOTHING;
