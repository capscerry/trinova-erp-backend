-- Migration: Add nomor_faktur_pajak to purchase_invoice
-- Run this script once against your database before deploying the updated backend.
-- The column is nullable so existing rows are unaffected.

ALTER TABLE purchase_invoice
    ADD nomor_faktur_pajak NVARCHAR(50) NULL;
