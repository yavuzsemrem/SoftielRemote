-- FAZ 4.1 - Tüm tabloları tek seferde oluştur
-- PostgreSQL (Supabase) uyumlu
-- Bu script'i Supabase SQL Editor'de çalıştırabilirsiniz

-- 1. devices tablosu
CREATE TABLE IF NOT EXISTS devices (
  id uuid PRIMARY KEY,
  owner_user_id uuid NULL,
  device_code text NOT NULL UNIQUE,
  device_name text NOT NULL,
  device_type text NOT NULL,
  is_online boolean NOT NULL DEFAULT false,
  last_seen_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- devices index'ler
CREATE INDEX IF NOT EXISTS idx_devices_owner_user_id ON devices(owner_user_id);
CREATE INDEX IF NOT EXISTS idx_devices_device_code ON devices(device_code);

-- 2. sessions tablosu
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

-- sessions index'ler
CREATE INDEX IF NOT EXISTS idx_sessions_host_device_id ON sessions(host_device_id);
CREATE INDEX IF NOT EXISTS idx_sessions_client_device_id ON sessions(client_device_id);
CREATE INDEX IF NOT EXISTS idx_sessions_status ON sessions(status);

-- 3. Foreign key constraint'leri
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_sessions_host_device_id'
  ) THEN
    ALTER TABLE sessions
      ADD CONSTRAINT fk_sessions_host_device_id
      FOREIGN KEY (host_device_id)
      REFERENCES devices(id)
      ON DELETE RESTRICT;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_sessions_client_device_id'
  ) THEN
    ALTER TABLE sessions
      ADD CONSTRAINT fk_sessions_client_device_id
      FOREIGN KEY (client_device_id)
      REFERENCES devices(id)
      ON DELETE RESTRICT;
  END IF;
END $$;

-- 4. Status enum CHECK constraint
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'chk_sessions_status'
  ) THEN
    ALTER TABLE sessions
      ADD CONSTRAINT chk_sessions_status
      CHECK (status IN ('Created', 'PendingApproval', 'Approved', 'Rejected', 'Connected', 'Ended', 'Failed'));
  END IF;
END $$;

