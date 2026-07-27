-- ============================================================
-- CLEANUP: hapus semua data demo dari seed_sales_flow_accurate_demo_data.sql
-- (ditandai prefix FLOWDEMO- / SO-FLOWDEMO- / SQ-FLOWDEMO- / SJ-FLOWDEMO- /
--  INV-FLOWDEMO- / BP-FLOWDEMO- / UM-FLOWDEMO-)
-- Urutan hapus mengikuti arah foreign key (detail dulu, baru header).
-- ============================================================

DELETE FROM sales_receipt WHERE no_bukti LIKE 'BP-FLOWDEMO-%';

DELETE FROM uang_muka WHERE NoFaktur LIKE 'UM-FLOWDEMO-%';

DELETE FROM sales_invoice_detail WHERE sales_invoice_id IN (SELECT id FROM sales_invoice WHERE invoice_number LIKE 'INV-FLOWDEMO-%');
DELETE FROM sales_invoice WHERE invoice_number LIKE 'INV-FLOWDEMO-%';

DELETE FROM delivery_order_detail WHERE delivery_id IN (SELECT id FROM delivery_order_header WHERE do_number LIKE 'SJ-FLOWDEMO-%');
DELETE FROM delivery_order_header WHERE do_number LIKE 'SJ-FLOWDEMO-%';

DELETE FROM sales_order_detail WHERE order_id IN (SELECT order_id FROM sales_order WHERE so_number LIKE 'SO-FLOWDEMO-%');
DELETE FROM sales_order WHERE so_number LIKE 'SO-FLOWDEMO-%';

DELETE FROM quotation_detail WHERE quotation_id IN (SELECT quotation_id FROM sales_quotation WHERE quotation_number LIKE 'SQ-FLOWDEMO-%');
DELETE FROM sales_quotation WHERE quotation_number LIKE 'SQ-FLOWDEMO-%';

DELETE FROM master_customer WHERE customer_code LIKE 'FLOWDEMO-%';

PRINT 'Cleanup selesai — semua data flow-accurate demo (FLOWDEMO-/SO-FLOWDEMO-/dst) sudah dihapus.';
