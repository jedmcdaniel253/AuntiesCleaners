-- Migration: make email and phone nullable on owner table

ALTER TABLE owner ALTER COLUMN email DROP NOT NULL;
ALTER TABLE owner ALTER COLUMN phone DROP NOT NULL;
