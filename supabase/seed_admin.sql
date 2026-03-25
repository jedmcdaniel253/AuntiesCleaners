-- ============================================================
-- Seed Admin User
-- ============================================================
-- INSTRUCTIONS:
-- 1. First, create a user in Supabase Auth:
--    - Go to your Supabase Dashboard → Authentication → Users
--    - Click "Add user" → "Create new user"
--    - Enter your email and a password
--    - Copy the UUID shown for the new user
--
-- 2. Replace the placeholders below with your actual values:
--    - YOUR_AUTH_USER_UUID: the UUID from step 1
--    - Your Name: your actual name
--    - your@email.com: the email you used in step 1
--
-- 3. Run this script in Supabase SQL Editor (Dashboard → SQL Editor)
-- ============================================================

-- Step 1: Create a worker record for the admin
INSERT INTO workers (id, name, phone, is_active)
VALUES (
    '00000000-0000-0000-0000-000000000099',
    'Your Name',          -- ← Replace with your name
    null,
    true
);

-- Step 2: Create the user profile linked to the auth user
INSERT INTO user_profiles (id, auth_user_id, worker_id, name, email, role, is_active)
VALUES (
    '00000000-0000-0000-0000-000000000100',
    'YOUR_AUTH_USER_UUID', -- ← Replace with the UUID from Supabase Auth
    '00000000-0000-0000-0000-000000000099',
    'Your Name',           -- ← Replace with your name
    'your@email.com',      -- ← Replace with your email
    'Admin',
    true
);

-- Step 3: Set up default Admin tab configuration
INSERT INTO tab_configurations (user_profile_id, tab_name, display_order, is_visible) VALUES
    ('00000000-0000-0000-0000-000000000100', 'Cleaning', 1, true),
    ('00000000-0000-0000-0000-000000000100', 'Laundry', 2, true),
    ('00000000-0000-0000-0000-000000000100', 'Maintenance', 3, true),
    ('00000000-0000-0000-0000-000000000100', 'Lawn', 4, true),
    ('00000000-0000-0000-0000-000000000100', 'Calendar', 5, true),
    ('00000000-0000-0000-0000-000000000100', 'Mow List', 6, false),
    ('00000000-0000-0000-0000-000000000100', 'My Entries', 7, true),
    ('00000000-0000-0000-0000-000000000100', 'Daily Summary', 8, true),
    ('00000000-0000-0000-0000-000000000100', 'Receipts', 9, false),
    ('00000000-0000-0000-0000-000000000100', 'Rates', 10, false),
    ('00000000-0000-0000-0000-000000000100', 'Billing', 11, false),
    ('00000000-0000-0000-0000-000000000100', 'Worker Pay', 12, false),
    ('00000000-0000-0000-0000-000000000100', 'Owner', 13, false),
    ('00000000-0000-0000-0000-000000000100', 'Workers', 14, false),
    ('00000000-0000-0000-0000-000000000100', 'Houses', 15, false),
    ('00000000-0000-0000-0000-000000000100', 'Users', 16, false),
    ('00000000-0000-0000-0000-000000000100', 'Tab Config', 17, false);
