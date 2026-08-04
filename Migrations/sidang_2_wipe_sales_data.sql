-- ============================================================
-- LANGKAH 2/4 — WIPE seluruh data Sales module (transaksi + customer).
--
-- Menghapus: Sales Return, Sales Receipt, Sales Invoice, Delivery Order,
-- Uang Muka, Sales Order, Sales Quotation, DAN master_customer /
-- master_customer_category.
--
-- TIDAK menyentuh master data modul lain (produk, gudang, UOM, bank,
-- delivery_category) -- itu dipakai bareng modul lain, tetap dibiarkan.
--
-- WAJIB jalankan sidang_1_backup_before_wipe.sql DULU -- ini destruktif
-- dan tidak bisa di-undo kecuali lewat backup itu.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION WipeSalesForSidang;

BEGIN TRY

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

    -- Reset identity seed supaya insert berikutnya (data dummy) mulai
    -- dari ID 1 lagi -- hapus/komentari blok ini kalau tidak masalah
    -- dengan ID lanjut dari nomor lama.
    DBCC CHECKIDENT ('sales_return_header',       RESEED, 0);
    DBCC CHECKIDENT ('sales_return_detail',       RESEED, 0);
    DBCC CHECKIDENT ('sales_receipt',             RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice',             RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice_detail',      RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_header',     RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_detail',     RESEED, 0);
    DBCC CHECKIDENT ('uang_muka',                 RESEED, 0);
    DBCC CHECKIDENT ('sales_order',               RESEED, 0);
    DBCC CHECKIDENT ('sales_order_detail',        RESEED, 0);
    DBCC CHECKIDENT ('sales_quotation',           RESEED, 0);
    DBCC CHECKIDENT ('quotation_detail',          RESEED, 0);
    DBCC CHECKIDENT ('master_customer',           RESEED, 0);
    DBCC CHECKIDENT ('master_customer_category',  RESEED, 0);

    COMMIT TRANSACTION WipeSalesForSidang;

    PRINT 'Wipe selesai -- seluruh data Sales module (termasuk customer) sudah kosong.';
    PRINT 'CATATAN: qty stok (inventory_stock) yang sudah bergerak akibat DO/Return yang baru dihapus TIDAK ikut di-reset otomatis.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION WipeSalesForSidang;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
