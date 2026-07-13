-- Migration: add warehouse_id to sales_invoice_detail
-- Needed so an Invoice with no linked Sales Order/Delivery Order (a direct
-- cash sale) can deduct stock at the correct warehouse for physical-goods
-- lines. Lines left NULL are treated as services (jasa) and never touch stock.

IF COL_LENGTH('sales_invoice_detail', 'warehouse_id') IS NULL
BEGIN
    ALTER TABLE sales_invoice_detail
    ADD warehouse_id INT NULL;

    PRINT 'Column warehouse_id added to sales_invoice_detail.';
END
ELSE
BEGIN
    PRINT 'Column warehouse_id already exists, skipping.';
END
