-- Migration: add warehouse_id to delivery_order_detail
-- Needed so Delivery Order confirmation knows which warehouse to deduct
-- stock from per line (prefilled from the linked sales_order_detail line,
-- but editable in case the DO ships from a different warehouse).

IF COL_LENGTH('delivery_order_detail', 'warehouse_id') IS NULL
BEGIN
    ALTER TABLE delivery_order_detail
    ADD warehouse_id INT NULL;

    PRINT 'Column warehouse_id added to delivery_order_detail.';
END
ELSE
BEGIN
    PRINT 'Column warehouse_id already exists, skipping.';
END
