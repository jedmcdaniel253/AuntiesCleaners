-- Auntie's Cleaners: Initial Schema
-- Enums
CREATE TYPE work_category AS ENUM ('Cleaning', 'Laundry', 'Maintenance', 'Lawn');
CREATE TYPE user_role AS ENUM ('Worker', 'Manager', 'Boss', 'Admin');

-- Workers
CREATE TABLE workers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    phone TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Houses
CREATE TABLE houses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    is_multiple_houses BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- User Profiles (linked to Supabase Auth)
CREATE TABLE user_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL UNIQUE REFERENCES auth.users(id) ON DELETE CASCADE,
    worker_id UUID REFERENCES workers(id),
    name TEXT NOT NULL,
    email TEXT NOT NULL,
    role user_role NOT NULL DEFAULT 'Worker',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Work Entries
CREATE TABLE work_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id UUID NOT NULL REFERENCES workers(id),
    house_id UUID NOT NULL REFERENCES houses(id),
    work_category work_category NOT NULL,
    entry_date DATE NOT NULL,
    hours_billed DECIMAL(5,2),
    number_of_loads INTEGER,
    created_by UUID NOT NULL REFERENCES user_profiles(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_cleaning_maintenance_hours CHECK (
        (work_category IN ('Cleaning', 'Maintenance') AND hours_billed IS NOT NULL AND hours_billed > 0)
        OR work_category NOT IN ('Cleaning', 'Maintenance')
    ),
    CONSTRAINT chk_laundry_loads CHECK (
        (work_category = 'Laundry' AND number_of_loads IS NOT NULL AND number_of_loads > 0)
        OR work_category != 'Laundry'
    )
);

-- Miscellaneous Entries
CREATE TABLE miscellaneous_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id UUID NOT NULL REFERENCES workers(id),
    house_id UUID REFERENCES houses(id),
    entry_date DATE NOT NULL,
    description TEXT NOT NULL,
    charge_amount DECIMAL(10,2) NOT NULL CHECK (charge_amount > 0),
    pay_amount DECIMAL(10,2) NOT NULL CHECK (pay_amount > 0),
    created_by UUID NOT NULL REFERENCES user_profiles(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Receipts
CREATE TABLE receipts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id UUID NOT NULL REFERENCES workers(id),
    receipt_date DATE NOT NULL,
    business_name TEXT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    is_reimbursable BOOLEAN NOT NULL DEFAULT true,
    photo_url TEXT NOT NULL,
    created_by UUID NOT NULL REFERENCES user_profiles(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Rates (Cleaning/Maintenance hourly, Laundry per-load)
CREATE TABLE rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    work_category work_category NOT NULL,
    worker_id UUID REFERENCES workers(id),
    rate_charged DECIMAL(10,2) NOT NULL CHECK (rate_charged > 0),
    rate_paid DECIMAL(10,2) NOT NULL CHECK (rate_paid >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_rate UNIQUE (work_category, worker_id)
);

-- Lawn House Rates (flat per-instance per house)
CREATE TABLE lawn_house_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    house_id UUID NOT NULL REFERENCES houses(id),
    worker_id UUID REFERENCES workers(id),
    rate_charged DECIMAL(10,2) NOT NULL CHECK (rate_charged > 0),
    rate_paid DECIMAL(10,2) NOT NULL CHECK (rate_paid >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_lawn_rate UNIQUE (house_id, worker_id)
);

-- Turnover Events (Calendar)
CREATE TABLE turnover_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    house_id UUID NOT NULL REFERENCES houses(id),
    event_date DATE NOT NULL,
    is_checkout BOOLEAN NOT NULL DEFAULT false,
    is_checkin BOOLEAN NOT NULL DEFAULT false,
    notes TEXT,
    created_by UUID NOT NULL REFERENCES user_profiles(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_at_least_one CHECK (is_checkout OR is_checkin)
);

-- Mow List Items
CREATE TABLE mow_list_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    house_id UUID NOT NULL UNIQUE REFERENCES houses(id),
    needs_mowing BOOLEAN NOT NULL DEFAULT false,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Owner (singleton)
CREATE TABLE owner (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    phone TEXT NOT NULL DEFAULT '',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Tab Configurations
CREATE TABLE tab_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_profile_id UUID NOT NULL REFERENCES user_profiles(id) ON DELETE CASCADE,
    tab_name TEXT NOT NULL,
    display_order INTEGER NOT NULL,
    is_visible BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT uq_tab_config UNIQUE (user_profile_id, tab_name)
);

-- Indexes
CREATE INDEX idx_work_entries_worker ON work_entries(worker_id);
CREATE INDEX idx_work_entries_date ON work_entries(entry_date);
CREATE INDEX idx_work_entries_created_by ON work_entries(created_by);
CREATE INDEX idx_miscellaneous_entries_worker ON miscellaneous_entries(worker_id);
CREATE INDEX idx_miscellaneous_entries_date ON miscellaneous_entries(entry_date);
CREATE INDEX idx_receipts_worker ON receipts(worker_id);
CREATE INDEX idx_receipts_date ON receipts(receipt_date);
CREATE INDEX idx_turnover_events_date ON turnover_events(event_date);
CREATE INDEX idx_turnover_events_house ON turnover_events(house_id);
CREATE INDEX idx_user_profiles_auth ON user_profiles(auth_user_id);

-- Helper function to get current user's role
CREATE OR REPLACE FUNCTION get_user_role()
RETURNS user_role AS $$
    SELECT role FROM user_profiles WHERE auth_user_id = auth.uid();
$$ LANGUAGE sql SECURITY DEFINER STABLE;

-- Helper function to get current user's profile id
CREATE OR REPLACE FUNCTION get_user_profile_id()
RETURNS UUID AS $$
    SELECT id FROM user_profiles WHERE auth_user_id = auth.uid();
$$ LANGUAGE sql SECURITY DEFINER STABLE;

-- Row Level Security
ALTER TABLE workers ENABLE ROW LEVEL SECURITY;
ALTER TABLE houses ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE work_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE miscellaneous_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE receipts ENABLE ROW LEVEL SECURITY;
ALTER TABLE rates ENABLE ROW LEVEL SECURITY;
ALTER TABLE lawn_house_rates ENABLE ROW LEVEL SECURITY;
ALTER TABLE turnover_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE mow_list_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE owner ENABLE ROW LEVEL SECURITY;
ALTER TABLE tab_configurations ENABLE ROW LEVEL SECURITY;

-- Workers: all authenticated can read, Boss/Admin can write
CREATE POLICY workers_select ON workers FOR SELECT TO authenticated USING (true);
CREATE POLICY workers_insert ON workers FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY workers_update ON workers FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Houses: all authenticated can read, Boss/Admin can write
CREATE POLICY houses_select ON houses FOR SELECT TO authenticated USING (true);
CREATE POLICY houses_insert ON houses FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY houses_update ON houses FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- User Profiles: users can read own, Boss/Admin can read/write all
CREATE POLICY profiles_select ON user_profiles FOR SELECT TO authenticated USING (
    auth_user_id = auth.uid() OR get_user_role() IN ('Boss', 'Admin')
);
CREATE POLICY profiles_insert ON user_profiles FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY profiles_update ON user_profiles FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Work Entries: all authenticated can read/write (visibility filtered in app)
CREATE POLICY work_entries_select ON work_entries FOR SELECT TO authenticated USING (true);
CREATE POLICY work_entries_insert ON work_entries FOR INSERT TO authenticated WITH CHECK (true);
CREATE POLICY work_entries_update ON work_entries FOR UPDATE TO authenticated USING (true);
CREATE POLICY work_entries_delete ON work_entries FOR DELETE TO authenticated USING (true);

-- Miscellaneous Entries: all can read, Boss/Admin can write
CREATE POLICY misc_entries_select ON miscellaneous_entries FOR SELECT TO authenticated USING (true);
CREATE POLICY misc_entries_insert ON miscellaneous_entries FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY misc_entries_update ON miscellaneous_entries FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY misc_entries_delete ON miscellaneous_entries FOR DELETE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Receipts: all authenticated can read/write
CREATE POLICY receipts_select ON receipts FOR SELECT TO authenticated USING (true);
CREATE POLICY receipts_insert ON receipts FOR INSERT TO authenticated WITH CHECK (true);
CREATE POLICY receipts_update ON receipts FOR UPDATE TO authenticated USING (true);
CREATE POLICY receipts_delete ON receipts FOR DELETE TO authenticated USING (true);

-- Rates: all can read, Boss/Admin can write
CREATE POLICY rates_select ON rates FOR SELECT TO authenticated USING (true);
CREATE POLICY rates_insert ON rates FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY rates_update ON rates FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Lawn House Rates: all can read, Boss/Admin can write
CREATE POLICY lawn_rates_select ON lawn_house_rates FOR SELECT TO authenticated USING (true);
CREATE POLICY lawn_rates_insert ON lawn_house_rates FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY lawn_rates_update ON lawn_house_rates FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Turnover Events: all can read, Manager/Boss/Admin can write
CREATE POLICY turnover_select ON turnover_events FOR SELECT TO authenticated USING (true);
CREATE POLICY turnover_insert ON turnover_events FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Manager', 'Boss', 'Admin'));
CREATE POLICY turnover_update ON turnover_events FOR UPDATE TO authenticated USING (get_user_role() IN ('Manager', 'Boss', 'Admin'));
CREATE POLICY turnover_delete ON turnover_events FOR DELETE TO authenticated USING (get_user_role() IN ('Manager', 'Boss', 'Admin'));

-- Mow List: all can read, Manager/Boss/Admin can write
CREATE POLICY mow_list_select ON mow_list_items FOR SELECT TO authenticated USING (true);
CREATE POLICY mow_list_update ON mow_list_items FOR UPDATE TO authenticated USING (get_user_role() IN ('Manager', 'Boss', 'Admin'));
CREATE POLICY mow_list_insert ON mow_list_items FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Manager', 'Boss', 'Admin'));

-- Owner: Boss/Admin can read/write
CREATE POLICY owner_select ON owner FOR SELECT TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY owner_update ON owner FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Tab Configurations: users can read own, Boss/Admin can read/write all
CREATE POLICY tab_config_select ON tab_configurations FOR SELECT TO authenticated USING (
    user_profile_id = get_user_profile_id() OR get_user_role() IN ('Boss', 'Admin')
);
CREATE POLICY tab_config_insert ON tab_configurations FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY tab_config_update ON tab_configurations FOR UPDATE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY tab_config_delete ON tab_configurations FOR DELETE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));
