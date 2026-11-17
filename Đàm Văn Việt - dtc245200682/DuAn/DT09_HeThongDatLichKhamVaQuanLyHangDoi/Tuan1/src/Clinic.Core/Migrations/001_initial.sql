CREATE TABLE IF NOT EXISTS doctors (
 id integer PRIMARY KEY, name text NOT NULL, specialty text NOT NULL, room text NOT NULL
);
CREATE TABLE IF NOT EXISTS slots (
 id uuid PRIMARY KEY, doctor_id integer NOT NULL REFERENCES doctors(id),
 visit_date date NOT NULL, starts_at timestamptz NOT NULL, ends_at timestamptz NOT NULL,
 capacity integer NOT NULL CHECK(capacity > 0), reserved integer NOT NULL DEFAULT 0,
 CHECK(reserved >= 0 AND reserved <= capacity), CHECK(ends_at > starts_at),
 UNIQUE(doctor_id, starts_at), UNIQUE(id, doctor_id, visit_date)
);
CREATE TABLE IF NOT EXISTS patients (
 id uuid PRIMARY KEY, name text NOT NULL, phone text NOT NULL
);
CREATE TABLE IF NOT EXISTS doctor_queues (
 doctor_id integer NOT NULL REFERENCES doctors(id), visit_date date NOT NULL,
 last_number integer NOT NULL DEFAULT 0, PRIMARY KEY(doctor_id, visit_date)
);
CREATE TABLE IF NOT EXISTS appointments (
 id uuid PRIMARY KEY, patient_id uuid NOT NULL REFERENCES patients(id),
 slot_id uuid NOT NULL, doctor_id integer NOT NULL, visit_date date NOT NULL,
 token_hash text NOT NULL, number integer,
 status text NOT NULL CHECK(status IN ('Pending','Waiting','Serving','Completed','Cancelled','Expired')),
 created_at timestamptz NOT NULL DEFAULT now(),
 FOREIGN KEY(slot_id, doctor_id, visit_date) REFERENCES slots(id, doctor_id, visit_date),
 UNIQUE(doctor_id, visit_date, number),
 CHECK(number IS NULL OR number > 0),
 CHECK(status NOT IN ('Waiting','Serving','Completed') OR number IS NOT NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS one_serving_per_queue ON appointments(doctor_id, visit_date) WHERE status = 'Serving';
CREATE INDEX IF NOT EXISTS queue_lookup ON appointments(doctor_id, visit_date, status);
CREATE TABLE IF NOT EXISTS outbox (
 id uuid PRIMARY KEY, kind text NOT NULL, body text NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), sent_at timestamptz
);
CREATE INDEX IF NOT EXISTS pending_outbox ON outbox(created_at) WHERE sent_at IS NULL;
