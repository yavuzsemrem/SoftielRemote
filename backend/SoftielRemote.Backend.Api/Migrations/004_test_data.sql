-- FAZ 4.1 - Test SQL'leri
-- Bu script test amaçlıdır, production'da çalıştırılmamalıdır

-- 1. Device insert test
INSERT INTO devices (id, owner_user_id, device_code, device_name, device_type, is_online, last_seen_at, created_at)
VALUES 
  (
    gen_random_uuid(),
    NULL,
    '123 456 789',
    'Test Host Device',
    'Windows',
    true,
    now(),
    now()
  ),
  (
    gen_random_uuid(),
    NULL,
    '987 654 321',
    'Test Client Device',
    'Mac',
    true,
    now(),
    now()
  )
ON CONFLICT (device_code) DO NOTHING;

-- 2. Session request test (PendingApproval durumunda)
-- Önce device'ları alalım
DO $$
DECLARE
  host_device_id_val uuid;
  client_device_id_val uuid;
  session_id_val uuid;
BEGIN
  -- Host device'ı bul
  SELECT id INTO host_device_id_val FROM devices WHERE device_code = '123 456 789' LIMIT 1;
  
  -- Client device'ı bul
  SELECT id INTO client_device_id_val FROM devices WHERE device_code = '987 654 321' LIMIT 1;
  
  -- Session oluştur
  session_id_val := gen_random_uuid();
  
  INSERT INTO sessions (id, host_device_id, client_device_id, status, created_at)
  VALUES (session_id_val, host_device_id_val, client_device_id_val, 'PendingApproval', now());
  
  RAISE NOTICE 'Session created: %', session_id_val;
END $$;

-- 3. PendingApproval → Approved geçişi test
DO $$
DECLARE
  session_id_val uuid;
BEGIN
  -- En son oluşturulan session'ı al
  SELECT id INTO session_id_val 
  FROM sessions 
  WHERE status = 'PendingApproval' 
  ORDER BY created_at DESC 
  LIMIT 1;
  
  IF session_id_val IS NOT NULL THEN
    UPDATE sessions
    SET status = 'Approved', approved_at = now()
    WHERE id = session_id_val;
    
    RAISE NOTICE 'Session approved: %', session_id_val;
  ELSE
    RAISE NOTICE 'No pending session found';
  END IF;
END $$;

-- 4. Invalid state transition test (bu hata vermeli)
-- Bu sorgu CHECK constraint nedeniyle başarısız olmalı
-- DO $$
-- BEGIN
--   UPDATE sessions
--   SET status = 'InvalidStatus'
--   WHERE status = 'PendingApproval'
--   LIMIT 1;
-- END $$;

-- 5. Session listesi (test için)
SELECT 
  s.id,
  s.status,
  s.created_at,
  h.device_code AS host_device_code,
  c.device_code AS client_device_code
FROM sessions s
LEFT JOIN devices h ON s.host_device_id = h.id
LEFT JOIN devices c ON s.client_device_id = c.id
ORDER BY s.created_at DESC
LIMIT 10;

