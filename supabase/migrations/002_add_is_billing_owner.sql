-- Migration: add is_billing_owner to owner table
-- Requirements: 7.1, 8.1, 8.2, 8.3

-- Add the is_billing_owner column with a default of false
ALTER TABLE owner ADD COLUMN is_billing_owner boolean NOT NULL DEFAULT false;

-- Preserve existing singleton owner as billing owner (if any)
UPDATE owner SET is_billing_owner = true
WHERE id = (SELECT id FROM owner ORDER BY updated_at ASC LIMIT 1);

-- Add RLS policies for owner insert and delete (needed for full CRUD)
CREATE POLICY owner_insert ON owner FOR INSERT TO authenticated WITH CHECK (get_user_role() IN ('Boss', 'Admin'));
CREATE POLICY owner_delete ON owner FOR DELETE TO authenticated USING (get_user_role() IN ('Boss', 'Admin'));

-- Add owner_id foreign key to houses table for owner assignment
ALTER TABLE houses ADD COLUMN owner_id UUID REFERENCES owner(id) ON DELETE SET NULL;
