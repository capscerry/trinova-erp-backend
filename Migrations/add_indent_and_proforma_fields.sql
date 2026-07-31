-- Migration: add is_indent to sales_order + proforma_stage to sales_invoice
-- Supports the new indent-goods flow: indent orders require 2 Sales Invoices
-- (Proforma DP 30% then Proforma Pelunasan 70%) before a Delivery Order can
-- be created. is_indent is a UI guide only — the core rule "DO may only be
-- created after all SO invoices are fully paid" applies to both flow types.

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
