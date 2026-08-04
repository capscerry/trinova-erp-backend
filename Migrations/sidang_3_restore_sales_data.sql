-- ============================================================
-- LANGKAH 3/4 (OPSIONAL, JALANKAN HANYA KALAU PERLU) — RESTORE data
-- Sales module persis seperti sebelum sidang_2_wipe_sales_data.sql,
-- menggunakan snapshot yang dibuat oleh sidang_1_backup_before_wipe.sql.
--
-- Aman dijalankan kapan saja setelah backup ada di schema sales_backup --
-- akan MENGHAPUS dulu apa pun yang ada sekarang di tabel dbo.* (termasuk
-- data dummy hasil sidang_4/sidang_5 kalau sudah sempat dijalankan), baru
-- mengisi ulang dari backup dengan ID persis sama seperti semula.
-- ============================================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sales_backup')
BEGIN
    RAISERROR('Tidak ada schema sales_backup -- jalankan sidang_1_backup_before_wipe.sql dulu sebelum restore.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION RestoreSalesForSidang;

BEGIN TRY

    -- 1. Kosongkan dulu tabel dbo.* saat ini (child -> parent)
    DELETE FROM sales_return_detail;
    DELETE FROM sales_return_header;
    DELETE FROM sales_receipt;
    DELETE FROM sales_invoice_detail;
    DELETE FROM sales_invoice;
    DELETE FROM delivery_order_detail;
    DELETE FROM delivery_order_header;
    DELETE FROM uang_muka;
    DELETE FROM sales_order_detail;
    DELETE FROM sales_order;
    DELETE FROM quotation_detail;
    DELETE FROM sales_quotation;
    DELETE FROM master_customer;
    DELETE FROM master_customer_category;

    -- 2. Isi ulang dari sales_backup.* (parent -> child), pakai
    --    IDENTITY_INSERT supaya ID persis sama seperti data asli --
    --    dicek dulu per tabel apakah kolom identity-nya benar ada,
    --    supaya tidak error kalau ternyata tabelnya bukan identity table.

    IF OBJECTPROPERTY(OBJECT_ID('dbo.master_customer_category'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.master_customer_category ON;
    INSERT INTO dbo.master_customer_category SELECT * FROM sales_backup.master_customer_category;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.master_customer_category'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.master_customer_category OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.master_customer'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.master_customer ON;
    INSERT INTO dbo.master_customer SELECT * FROM sales_backup.master_customer;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.master_customer'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.master_customer OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_quotation'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_quotation ON;
    INSERT INTO dbo.sales_quotation SELECT * FROM sales_backup.sales_quotation;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_quotation'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_quotation OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.quotation_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.quotation_detail ON;
    INSERT INTO dbo.quotation_detail SELECT * FROM sales_backup.quotation_detail;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.quotation_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.quotation_detail OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_order'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_order ON;
    INSERT INTO dbo.sales_order SELECT * FROM sales_backup.sales_order;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_order'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_order OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_order_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_order_detail ON;
    INSERT INTO dbo.sales_order_detail SELECT * FROM sales_backup.sales_order_detail;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_order_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_order_detail OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.uang_muka'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.uang_muka ON;
    INSERT INTO dbo.uang_muka SELECT * FROM sales_backup.uang_muka;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.uang_muka'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.uang_muka OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.delivery_order_header'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.delivery_order_header ON;
    INSERT INTO dbo.delivery_order_header SELECT * FROM sales_backup.delivery_order_header;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.delivery_order_header'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.delivery_order_header OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.delivery_order_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.delivery_order_detail ON;
    INSERT INTO dbo.delivery_order_detail SELECT * FROM sales_backup.delivery_order_detail;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.delivery_order_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.delivery_order_detail OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_invoice'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_invoice ON;
    INSERT INTO dbo.sales_invoice SELECT * FROM sales_backup.sales_invoice;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_invoice'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_invoice OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_invoice_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_invoice_detail ON;
    INSERT INTO dbo.sales_invoice_detail SELECT * FROM sales_backup.sales_invoice_detail;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_invoice_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_invoice_detail OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_receipt'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_receipt ON;
    INSERT INTO dbo.sales_receipt SELECT * FROM sales_backup.sales_receipt;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_receipt'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_receipt OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_return_header'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_return_header ON;
    INSERT INTO dbo.sales_return_header SELECT * FROM sales_backup.sales_return_header;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_return_header'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_return_header OFF;

    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_return_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_return_detail ON;
    INSERT INTO dbo.sales_return_detail SELECT * FROM sales_backup.sales_return_detail;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.sales_return_detail'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.sales_return_detail OFF;

    COMMIT TRANSACTION RestoreSalesForSidang;

    PRINT 'Restore selesai -- data Sales module sudah dikembalikan persis seperti sebelum wipe (ID sama).';
    PRINT 'CATATAN: qty stok (inventory_stock) tidak ikut direstore -- kalau sempat berubah gara-gara data dummy, cek/sesuaikan manual.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION RestoreSalesForSidang;
    PRINT 'Terjadi error, transaksi di-rollback. Data backup di schema sales_backup TIDAK hilang, aman dicoba lagi:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
