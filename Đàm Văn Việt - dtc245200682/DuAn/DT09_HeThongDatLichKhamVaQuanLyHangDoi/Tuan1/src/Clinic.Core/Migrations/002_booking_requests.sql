CREATE TABLE booking_requests (
 key_hash text PRIMARY KEY,
 request_hash text NOT NULL,
 appointment_id uuid NOT NULL UNIQUE REFERENCES appointments(id) ON DELETE CASCADE,
 created_at timestamptz NOT NULL DEFAULT now()
);
