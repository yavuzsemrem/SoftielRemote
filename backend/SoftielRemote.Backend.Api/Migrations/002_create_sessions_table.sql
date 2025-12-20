-- FAZ 4.1 - sessions tablosu
-- PostgreSQL (Supabase) uyumlu

CREATE TABLE IF NOT EXISTS sessions (
  id uuid PRIMARY KEY,
  host_device_id uuid NOT NULL,
  client_device_id uuid NULL,
  status text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  approved_at timestamptz NULL,
  connected_at timestamptz NULL,
  ended_at timestamptz NULL,
  end_reason text NULL
);

-- Index'ler
CREATE INDEX IF NOT EXISTS idx_sessions_host_device_id ON sessions(host_device_id);
CREATE INDEX IF NOT EXISTS idx_sessions_client_device_id ON sessions(client_device_id);
CREATE INDEX IF NOT EXISTS idx_sessions_status ON sessions(status);

-- Foreign key constraint'leri
ALTER TABLE sessions
  ADD CONSTRAINT fk_sessions_host_device_id
  FOREIGN KEY (host_device_id)
  REFERENCES devices(id)
  ON DELETE RESTRICT;

ALTER TABLE sessions
  ADD CONSTRAINT fk_sessions_client_device_id
  FOREIGN KEY (client_device_id)
  REFERENCES devices(id)
  ON DELETE RESTRICT;

-- Status enum CHECK constraint
ALTER TABLE sessions
  ADD CONSTRAINT chk_sessions_status
  CHECK (status IN ('Created', 'PendingApproval', 'Approved', 'Rejected', 'Connected', 'Ended', 'Failed'));

