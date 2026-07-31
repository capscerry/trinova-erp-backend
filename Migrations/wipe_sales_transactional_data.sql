-- ============================================================
-- WIPE: remove ALL Sales transactional data (not just demo/seed rows).
-- Covers: Quotation, Sales Order, Delivery Order, Uang Muka,
--         Sales Receipt, Sales Invoice, Sales Return.
--
-- Does NOT touch master data (customers, products, warehouses, etc.).
-- Does NOT reset inventory stock quantities moved by DO/Return rows
-- that are deleted — see note at end of file.
--
-- DELETE order follows foreign-key direction (children first):
--   sales_return_detail  -> sales_return_header
--   sales_receipt
--   sales_invoice_detail -> sales_invoice
--   delivery_order_detail -> delivery_order_header
--   uang_muka
--   sales_order_detail   -> sales_order
--   quotation_detail     -> sales_quotation
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

    -- Reset identity seeds so next inserts start from ID 1
    -- (optional — comment out if you prefer IDs to continue from old values)
    DBCC CHECKIDENT ('sales_return_header',  RESEED, 0);
    DBCC CHECKIDENT ('sales_return_detail',  RESEED, 0);
    DBCC CHECKIDENT ('sales_receipt',        RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice',        RESEED, 0);
    DBCC CHECKIDENT ('sales_invoice_detail', RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_header',RESEED, 0);
    DBCC CHECKIDENT ('delivery_order_detail',RESEED, 0);
    DBCC CHECKIDENT ('uang_muka',            RESEED, 0);
    DBCC CHECKIDENT ('sales_order',          RESEED, 0);
    DBCC CHECKIDENT ('sales_order_detail',   RESEED, 0);
    DBCC CHECKIDENT ('sales_quotation',      RESEED, 0);
    DBCC CHECKIDENT ('quotation_detail',     RESEED, 0);

    COMMIT TRANSACTION WipeSalesData;

    PRINT 'Wipe complete — sales_quotation, sales_order, delivery_order, uang_muka, sales_receipt, sales_invoice, sales_return are now empty.';
    PRINT 'Master data (customer/product/warehouse) was NOT touched.';
    PRINT 'NOTE: inventory stock quantities already moved by deleted DO/Return rows are NOT reset — check inventory_stock manually if a reset is also needed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION WipeSalesData;
    PRINT 'Error occurred, transaction rolled back:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
