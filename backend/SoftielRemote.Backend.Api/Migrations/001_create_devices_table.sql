-- FAZ 4.1 - devices tablosu
-- PostgreSQL (Supabase) uyumlu

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

-- Index'ler
CREATE INDEX IF NOT EXISTS idx_devices_owner_user_id ON devices(owner_user_id);
CREATE INDEX IF NOT EXISTS idx_devices_device_code ON devices(device_code);

-- device_code zaten UNIQUE constraint ile korunuyor, ama index de ekliyoruz performans için

