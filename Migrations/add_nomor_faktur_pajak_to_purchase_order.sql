-- Migration: Add nomor_faktur_pajak to purchase_order header
-- Run once against the database before deploying the updated backend.
ALTER TABLE purchase_order
    ADD nomor_faktur_pajak NVARCHAR(50) NULL;
