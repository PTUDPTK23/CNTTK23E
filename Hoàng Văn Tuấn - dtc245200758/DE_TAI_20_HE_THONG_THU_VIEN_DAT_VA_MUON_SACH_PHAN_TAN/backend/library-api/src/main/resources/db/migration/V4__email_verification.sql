CREATE TABLE email_verification_codes (
    email VARCHAR(150) PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    phone VARCHAR(20),
    password_hash VARCHAR(100) NOT NULL,
    code_hash VARCHAR(100) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    attempts INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_sent_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_email_verification_attempts CHECK (attempts >= 0 AND attempts <= 10)
);

CREATE INDEX idx_email_verification_expires
    ON email_verification_codes (expires_at);
