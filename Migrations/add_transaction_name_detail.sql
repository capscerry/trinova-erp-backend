-- Migration: Add transaction_name and transaction_detail to purchasing tables
-- Run this script once against your database before deploying the updated backend.
-- Both columns are nullable to avoid breaking existing rows.

-- 1. Purchase Order (entry point — user manually inputs these values here)
ALTER TABLE purchase_order
    ADD transaction_name   NVARCHAR(255) NULL,
        transaction_detail NVARCHAR(MAX) NULL;

-- 2. Purchase Return (denormalized copy, propagated from PO at insert time)
ALTER TABLE purchase_return
    ADD transaction_name   NVARCHAR(255) NULL,
        transaction_detail NVARCHAR(MAX) NULL;

-- NOTE: Purchase Down Payment, Goods Receipt, Purchase Invoice, and
-- Purchase Payment do NOT store these columns in the database.
-- They read transaction_name and transaction_detail at query time by
-- JOINing back to purchase_order through their existing FK chain.

-- Add expected_date (Tanggal Ekspektasi) to purchase_order
ALTER TABLE purchase_order
    ADD expected_date DATE NULL;
