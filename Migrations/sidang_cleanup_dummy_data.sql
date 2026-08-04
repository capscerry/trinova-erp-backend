-- ============================================================
-- (OPSIONAL) CLEANUP: hapus SEMUA data dummy dari sidang_4/sidang_5
-- (ditandai prefix SIDANG- / SO-SIDANG- / SQ-SIDANG- / SJ-SIDANG- /
--  INV-SIDANG- / BP-SIDANG- / UM-SIDANG-), TANPA perlu restore penuh
-- dari backup. Cocok kalau cuma mau buang data dummy-nya saja tapi
-- database masih kosong / belum ada data asli untuk direstore.
--
-- Kalau maksudnya justru mau balikin data ASLI (sebelum wipe), pakai
-- sidang_3_restore_sales_data.sql, bukan file ini.
-- ============================================================

SET NOCOUNT ON;

DELETE FROM sales_receipt WHERE no_bukti LIKE 'BP-SIDANG-%';

DELETE FROM uang_muka WHERE NoFaktur LIKE 'UM-SIDANG-%';

DELETE FROM sales_invoice_detail WHERE sales_invoice_id IN (SELECT id FROM sales_invoice WHERE invoice_number LIKE 'INV-SIDANG-%');
DELETE FROM sales_invoice WHERE invoice_number LIKE 'INV-SIDANG-%';

DELETE FROM delivery_order_detail WHERE delivery_id IN (SELECT id FROM delivery_order_header WHERE do_number LIKE 'SJ-SIDANG-%');
DELETE FROM delivery_order_header WHERE do_number LIKE 'SJ-SIDANG-%';

DELETE FROM sales_order_detail WHERE order_id IN (SELECT order_id FROM sales_order WHERE so_number LIKE 'SO-SIDANG-%');
DELETE FROM sales_order WHERE so_number LIKE 'SO-SIDANG-%';

DELETE FROM quotation_detail WHERE quotation_id IN (SELECT quotation_id FROM sales_quotation WHERE quotation_number LIKE 'SQ-SIDANG-%');
DELETE FROM sales_quotation WHERE quotation_number LIKE 'SQ-SIDANG-%';

DELETE FROM master_customer WHERE customer_code LIKE 'SIDANG-%';

PRINT 'Cleanup selesai -- semua data dummy sidang (SIDANG-/SO-SIDANG-/dst) sudah dihapus.';
PRINT 'CATATAN: master_customer_category (Retail/Distributor/dst) TIDAK ikut dihapus karena namanya generik -- hapus manual kalau memang mau.';
