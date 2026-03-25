-- Seed: Permanent "Multiple Houses" entry (cannot be deactivated or deleted)
INSERT INTO houses (id, name, is_multiple_houses, is_active)
VALUES ('00000000-0000-0000-0000-000000000001', 'Multiple Houses', true, true)
ON CONFLICT (id) DO NOTHING;

-- Seed: Singleton Owner record
INSERT INTO owner (id, name, email, phone)
VALUES ('00000000-0000-0000-0000-000000000002', '', '', '')
ON CONFLICT (id) DO NOTHING;
