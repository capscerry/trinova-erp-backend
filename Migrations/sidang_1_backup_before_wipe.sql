-- ============================================================
-- LANGKAH 1/4 — BACKUP seluruh data Sales module SEBELUM di-wipe.
--
-- Menyalin isi tabel dbo.* saat ini (siapa pun yang membuatnya --
-- bukan cuma data demo) ke schema "sales_backup", supaya bisa
-- dikembalikan persis seperti semula lewat sidang_3_restore_sales_data.sql
-- kalau ternyata dibutuhkan lagi setelah sidang.
--
-- WAJIB dijalankan SEBELUM sidang_2_wipe_sales_data.sql.
-- Aman dijalankan berkali-kali (tabel backup lama di-drop dulu).
-- ============================================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sales_backup')
BEGIN
    EXEC('CREATE SCHEMA sales_backup');
END
GO

IF OBJECT_ID('sales_backup.sales_return_detail', 'U') IS NOT NULL DROP TABLE sales_backup.sales_return_detail;
IF OBJECT_ID('sales_backup.sales_return_header', 'U') IS NOT NULL DROP TABLE sales_backup.sales_return_header;
IF OBJECT_ID('sales_backup.sales_receipt', 'U') IS NOT NULL DROP TABLE sales_backup.sales_receipt;
IF OBJECT_ID('sales_backup.sales_invoice_detail', 'U') IS NOT NULL DROP TABLE sales_backup.sales_invoice_detail;
IF OBJECT_ID('sales_backup.sales_invoice', 'U') IS NOT NULL DROP TABLE sales_backup.sales_invoice;
IF OBJECT_ID('sales_backup.delivery_order_detail', 'U') IS NOT NULL DROP TABLE sales_backup.delivery_order_detail;
IF OBJECT_ID('sales_backup.delivery_order_header', 'U') IS NOT NULL DROP TABLE sales_backup.delivery_order_header;
IF OBJECT_ID('sales_backup.uang_muka', 'U') IS NOT NULL DROP TABLE sales_backup.uang_muka;
IF OBJECT_ID('sales_backup.sales_order_detail', 'U') IS NOT NULL DROP TABLE sales_backup.sales_order_detail;
IF OBJECT_ID('sales_backup.sales_order', 'U') IS NOT NULL DROP TABLE sales_backup.sales_order;
IF OBJECT_ID('sales_backup.quotation_detail', 'U') IS NOT NULL DROP TABLE sales_backup.quotation_detail;
IF OBJECT_ID('sales_backup.sales_quotation', 'U') IS NOT NULL DROP TABLE sales_backup.sales_quotation;
IF OBJECT_ID('sales_backup.master_customer', 'U') IS NOT NULL DROP TABLE sales_backup.master_customer;
IF OBJECT_ID('sales_backup.master_customer_category', 'U') IS NOT NULL DROP TABLE sales_backup.master_customer_category;
GO

SELECT * INTO sales_backup.sales_return_detail   FROM dbo.sales_return_detail;
SELECT * INTO sales_backup.sales_return_header    FROM dbo.sales_return_header;
SELECT * INTO sales_backup.sales_receipt          FROM dbo.sales_receipt;
SELECT * INTO sales_backup.sales_invoice_detail   FROM dbo.sales_invoice_detail;
SELECT * INTO sales_backup.sales_invoice          FROM dbo.sales_invoice;
SELECT * INTO sales_backup.delivery_order_detail  FROM dbo.delivery_order_detail;
SELECT * INTO sales_backup.delivery_order_header  FROM dbo.delivery_order_header;
SELECT * INTO sales_backup.uang_muka              FROM dbo.uang_muka;
SELECT * INTO sales_backup.sales_order_detail     FROM dbo.sales_order_detail;
SELECT * INTO sales_backup.sales_order            FROM dbo.sales_order;
SELECT * INTO sales_backup.quotation_detail       FROM dbo.quotation_detail;
SELECT * INTO sales_backup.sales_quotation        FROM dbo.sales_quotation;
SELECT * INTO sales_backup.master_customer        FROM dbo.master_customer;
SELECT * INTO sales_backup.master_customer_category FROM dbo.master_customer_category;
GO

PRINT 'Backup selesai. Isi saat ini:';
SELECT 'sales_return_header'       AS tbl, COUNT(*) AS jumlah FROM sales_backup.sales_return_header
UNION ALL SELECT 'sales_receipt',          COUNT(*) FROM sales_backup.sales_receipt
UNION ALL SELECT 'sales_invoice',          COUNT(*) FROM sales_backup.sales_invoice
UNION ALL SELECT 'delivery_order_header',  COUNT(*) FROM sales_backup.delivery_order_header
UNION ALL SELECT 'uang_muka',              COUNT(*) FROM sales_backup.uang_muka
UNION ALL SELECT 'sales_order',            COUNT(*) FROM sales_backup.sales_order
UNION ALL SELECT 'sales_quotation',        COUNT(*) FROM sales_backup.sales_quotation
UNION ALL SELECT 'master_customer',        COUNT(*) FROM sales_backup.master_customer
UNION ALL SELECT 'master_customer_category', COUNT(*) FROM sales_backup.master_customer_category;
