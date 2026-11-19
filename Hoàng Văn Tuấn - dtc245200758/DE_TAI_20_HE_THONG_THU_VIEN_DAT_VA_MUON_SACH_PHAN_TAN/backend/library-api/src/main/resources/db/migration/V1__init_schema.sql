-- Schema chính của hệ thống thư viện (do Library API quản lý bằng Flyway).

CREATE TABLE users (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name     VARCHAR(150) NOT NULL,
    email         VARCHAR(150) NOT NULL,
    phone         VARCHAR(20),
    password_hash VARCHAR(100),
    role          VARCHAR(20)  NOT NULL DEFAULT 'USER',
    status        VARCHAR(20)  NOT NULL DEFAULT 'ACTIVE',
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_role CHECK (role IN ('ADMIN','LIBRARIAN','USER')),
    CONSTRAINT ck_users_status CHECK (status IN ('ACTIVE','LOCKED'))
);

CREATE TABLE categories (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_categories_name UNIQUE (name)
);

CREATE TABLE books (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    isbn             VARCHAR(20),
    title            VARCHAR(255) NOT NULL,
    author           VARCHAR(150) NOT NULL,
    publisher        VARCHAR(150),
    publication_year INT,
    category_id      UUID NOT NULL,
    description      TEXT,
    cover_image      VARCHAR(500),
    active           BOOLEAN NOT NULL DEFAULT TRUE,   -- xóa mềm: giữ lịch sử đặt mượn
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_books_isbn UNIQUE (isbn),            -- NULL được phép lặp, giá trị thì duy nhất
    CONSTRAINT fk_books_category FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE RESTRICT,
    CONSTRAINT ck_books_title CHECK (length(trim(title)) > 0),
    CONSTRAINT ck_books_year CHECK (publication_year IS NULL OR publication_year BETWEEN 1000 AND 2100)
);
CREATE INDEX idx_books_title ON books (title);
CREATE INDEX idx_books_author ON books (author);
CREATE INDEX idx_books_category ON books (category_id);

CREATE TABLE book_copies (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    book_id    UUID NOT NULL,
    copy_code  VARCHAR(50) NOT NULL,
    status     VARCHAR(20) NOT NULL DEFAULT 'AVAILABLE',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_copies_code UNIQUE (copy_code),
    CONSTRAINT fk_copies_book FOREIGN KEY (book_id) REFERENCES books(id) ON DELETE RESTRICT,
    CONSTRAINT ck_copies_status CHECK (status IN ('AVAILABLE','RESERVED','BORROWED','MAINTENANCE','LOST'))
);
CREATE INDEX idx_copies_book_status ON book_copies (book_id, status);

CREATE TABLE reservations (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID NOT NULL,
    book_id          UUID NOT NULL,
    book_copy_id     UUID,
    status           VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    request_id       UUID NOT NULL,
    requested_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    processed_at     TIMESTAMPTZ,
    borrowed_at      TIMESTAMPTZ,
    due_date         TIMESTAMPTZ,
    returned_at      TIMESTAMPTZ,
    rejection_reason VARCHAR(500),
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_reservations_request UNIQUE (request_id),
    CONSTRAINT fk_res_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_res_book FOREIGN KEY (book_id) REFERENCES books(id) ON DELETE RESTRICT,
    CONSTRAINT fk_res_copy FOREIGN KEY (book_copy_id) REFERENCES book_copies(id) ON DELETE RESTRICT,
    CONSTRAINT ck_res_status CHECK (status IN ('PENDING','RESERVED','REJECTED','BORROWED','RETURNED','CANCELLED')),
    -- RESERVED/BORROWED bắt buộc gắn với một bản sao
    CONSTRAINT ck_res_copy_required CHECK (status NOT IN ('RESERVED','BORROWED') OR book_copy_id IS NOT NULL)
);
CREATE INDEX idx_res_user ON reservations (user_id, requested_at DESC);
CREATE INDEX idx_res_book ON reservations (book_id);
CREATE INDEX idx_res_status ON reservations (status);

-- LỚP BẢO VỆ CUỐI CÙNG: một bản sao không thể được giữ/mượn bởi hai reservation cùng lúc,
-- kể cả khi code có lỗi.
CREATE UNIQUE INDEX uq_res_active_copy ON reservations (book_copy_id)
    WHERE book_copy_id IS NOT NULL AND status IN ('RESERVED','BORROWED');
-- Một người chỉ có tối đa một yêu cầu đang hoạt động cho mỗi đầu sách.
CREATE UNIQUE INDEX uq_res_active_user_book ON reservations (user_id, book_id)
    WHERE status IN ('PENDING','RESERVED','BORROWED');

-- Lịch sử đổi trạng thái (không xóa)
CREATE TABLE reservation_events (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id UUID NOT NULL,
    old_status     VARCHAR(20),
    new_status     VARCHAR(20) NOT NULL,
    note           VARCHAR(500),
    actor_type     VARCHAR(20) NOT NULL,
    actor_id       UUID,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT fk_events_res FOREIGN KEY (reservation_id) REFERENCES reservations(id) ON DELETE RESTRICT
);
CREATE INDEX idx_events_res ON reservation_events (reservation_id, created_at);

-- Outbox pattern: ghi cùng transaction với reservation, tiến trình publisher gửi sang RabbitMQ
CREATE TABLE message_outbox (
    id           UUID PRIMARY KEY,               -- = eventId của message
    aggregate_id UUID NOT NULL,
    event_type   VARCHAR(60) NOT NULL,
    payload      TEXT NOT NULL,
    status       VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    attempts     INT NOT NULL DEFAULT 0,
    last_error   VARCHAR(500),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    published_at TIMESTAMPTZ,
    CONSTRAINT ck_outbox_status CHECK (status IN ('PENDING','PUBLISHED'))
);
CREATE INDEX idx_outbox_pending ON message_outbox (created_at) WHERE status = 'PENDING';

-- Worker ghi nhận event đã xử lý để chống xử lý trùng (idempotency)
CREATE TABLE processed_events (
    event_id       UUID PRIMARY KEY,
    reservation_id UUID NOT NULL,
    processed_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);
