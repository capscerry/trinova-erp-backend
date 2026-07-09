-- Migration: Add nomor_faktur_pajak to purchase_order_detail
-- Run once against the database before deploying the updated backend.
-- Nullable so existing rows are unaffected.

ALTER TABLE purchase_order_detail
    ADD nomor_faktur_pajak NVARCHAR(50) NULL;
