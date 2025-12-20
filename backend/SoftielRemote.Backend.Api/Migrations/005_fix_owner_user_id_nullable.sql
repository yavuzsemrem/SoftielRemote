-- FAZ 4.3.1 - owner_user_id NULL yapılabilir hale getir
-- Veritabanında owner_user_id NOT NULL constraint'i varsa, bunu kaldır

-- Eğer constraint varsa kaldır
DO $$
BEGIN
  -- NOT NULL constraint'ini kaldır
  ALTER TABLE devices ALTER COLUMN owner_user_id DROP NOT NULL;
EXCEPTION
  WHEN OTHERS THEN
    -- Constraint yoksa hata verme, devam et
    NULL;
END $$;

-- Doğrulama: owner_user_id artık NULL olabilir
-- SELECT column_name, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_name = 'devices' AND column_name = 'owner_user_id';
-- is_nullable = 'YES' olmalı

