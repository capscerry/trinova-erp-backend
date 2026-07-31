-- Migration: add is_indent to sales_order + proforma_stage to sales_invoice
-- Mendukung flow baru: barang indent butuh 2x Sales Invoice berurutan
-- (Proforma DP 30% -> Proforma Pelunasan 70%) sebelum Delivery Order boleh
-- dibuat. is_indent murni menuntun UI (1 invoice reguler vs 2 invoice
-- proforma) -- aturan inti "DO baru bisa dibuat kalau semua invoice
-- terkait sudah lunas 100%" berlaku sama untuk kedua flow, jadi tidak ada
-- percabangan logika bisnis baru di sisi backend selain field label ini.

IF COL_LENGTH('sales_order', 'is_indent') IS NULL
BEGIN
    ALTER TABLE sales_order
    ADD is_indent BIT NOT NULL DEFAULT 0;

    PRINT 'Column is_indent added to sales_order.';
END
ELSE
BEGIN
    PRINT 'Column is_indent already exists on sales_order, skipping.';
END

IF COL_LENGTH('sales_invoice', 'proforma_stage') IS NULL
BEGIN
    ALTER TABLE sales_invoice
    ADD proforma_stage VARCHAR(20) NULL;

    PRINT 'Column proforma_stage added to sales_invoice.';
END
ELSE
BEGIN
    PRINT 'Column proforma_stage already exists on sales_invoice, skipping.';
END
