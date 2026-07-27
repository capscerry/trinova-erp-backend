-- ============================================================
-- CLEANUP: hapus semua data demo yang dibuat oleh
-- seed_sales_demo_data.sql (ditandai prefix DEMO- / SO-DEMO- / INV-DEMO-)
-- ============================================================

DELETE FROM sales_invoice_detail WHERE sales_invoice_id IN (SELECT id FROM sales_invoice WHERE invoice_number LIKE 'INV-DEMO-%');
DELETE FROM sales_invoice WHERE invoice_number LIKE 'INV-DEMO-%';
DELETE FROM sales_order_detail WHERE order_id IN (SELECT order_id FROM sales_order WHERE so_number LIKE 'SO-DEMO-%');
DELETE FROM sales_order WHERE so_number LIKE 'SO-DEMO-%';
DELETE FROM master_customer WHERE customer_code LIKE 'DEMO-%';

PRINT 'Cleanup selesai — semua data demo (DEMO-/SO-DEMO-/INV-DEMO-) sudah dihapus.';
