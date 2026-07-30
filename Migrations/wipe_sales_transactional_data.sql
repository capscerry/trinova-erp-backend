-- ============================================================
-- WIPE: hapus SELURUH data transaksi Sales (bukan cuma yang ditag
-- demo/seed) -- Quotation, Sales Order, Delivery Order, Uang Muka,
-- Sales Receipt, Sales Invoice, Sales Return.
--
-- TIDAK menyentuh master data (customer, produk, gudang, dst).
-- TIDAK mengembalikan qty stok yang sudah bergerak akibat DO/Return
-- yang dihapus -- lihat catatan di bagian bawah file ini.
--
-- Urutan DELETE mengikuti arah foreign key (child dulu, baru parent):
--   sales_return_detail -> sales_return_header
--   sales_receipt
--   sales_invoice_detail -> sales_invoice
--   delivery_order_detail -> delivery_order_header
--   uang_muka
--   sales_order_detail -> sales_order
--   quotation_detail -> sales_quotation
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION WipeSalesData;

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

    -- Reset identity seed supaya insert berikutnya mulai dari ID 1 lagi
    -- (opsional -- hapus/komentari blok ini kalau Anda tidak masalah
    -- dengan ID lanjut dari nomor lama).
    DBCC CHECKIDENT ('sales_return_header', RESEED, 0);
    DBCC CHECKIDENT ('sales_return_detail', RESEED, 0);
    DBCC CHECKIDENT ('sales_receipt', RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice', RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice_detail', RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_header', RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_detail', RESEED, 0);
    DBCC CHECKIDENT ('uang_muka', RESEED, 0);
    DBCC CHECKIDENT ('sales_order', RESEED, 0);
    DBCC CHECKIDENT ('sales_order_detail', RESEED, 0);
    DBCC CHECKIDENT ('sales_quotation', RESEED, 0);
    DBCC CHECKIDENT ('quotation_detail', RESEED, 0);

    COMMIT TRANSACTION WipeSalesData;

    PRINT 'Wipe selesai -- sales_quotation, sales_order, delivery_order, uang_muka, sales_receipt, sales_invoice, sales_return sudah kosong.';
    PRINT 'Master data (customer/produk/gudang) TIDAK disentuh.';
    PRINT 'CATATAN: qty stok yang sudah terpotong/kembali akibat DO/Return yang baru dihapus TIDAK ikut di-reset -- cek inventory_stock manual kalau perlu direset juga.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION WipeSalesData;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
