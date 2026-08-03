-- ============================================================
-- SEED SCRIPT: Sample Sales Data untuk Dashboard Testing
-- 30 customer baru, 150 sales_order, 120 sales_invoice
-- tersebar dalam 12 bulan terakhir
--
-- CATATAN:
-- - Nama kolom direkonstruksi dari query C# di Repositories/Penjualan/
--   (tidak ada CREATE TABLE resmi untuk sebagian tabel ini). Cek dulu
--   dengan `EXEC sp_help 'sales_order'` dsb. kalau ada error kolom.
-- - Semua data demo ditag jelas: customer_code LIKE 'DEMO-%',
--   so_number LIKE 'SO-DEMO-%', invoice_number LIKE 'INV-DEMO-%'.
-- - Dibungkus transaction + TRY/CATCH — kalau error, otomatis rollback.
-- - Lihat seed_sales_demo_data_cleanup.sql untuk menghapus data ini lagi.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION SeedSalesDemo;

BEGIN TRY

-- ------------------------------------------------------------
-- 0. Pastikan minimal ada 1 customer category
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM master_customer_category)
BEGIN
    INSERT INTO master_customer_category (category_name, is_active)
    VALUES ('Umum', 1);
END

-- ------------------------------------------------------------
-- 1. Seed 30 customer demo
-- ------------------------------------------------------------
DECLARE @CustomerNames TABLE (n INT IDENTITY(1,1), name NVARCHAR(200));
INSERT INTO @CustomerNames (name) VALUES
('PT Sinar Abadi Jaya'), ('CV Mitra Sejahtera'), ('PT Cahaya Nusantara'),
('Toko Makmur Bersama'), ('PT Global Prima Industri'), ('CV Berkah Teknik'),
('PT Sumber Rejeki Mandiri'), ('Toko Jaya Elektronik'), ('PT Karya Utama Logistik'),
('CV Anugrah Sejati'), ('PT Bintang Timur Sentosa'), ('Toko Sentosa Bangunan'),
('PT Maju Bersama Indonesia'), ('CV Harapan Baru'), ('PT Indo Perkasa Mandiri'),
('Toko Cendana Elektrik'), ('PT Wahana Sukses Abadi'), ('CV Delta Prima Sarana'),
('PT Nusantara Bahari'), ('Toko Barokah Jaya'), ('PT Kencana Mulia'),
('CV Trijaya Perkasa'), ('PT Pratama Global Sukses'), ('Toko Alam Sejahtera'),
('PT Cipta Karya Sentosa'), ('CV Warna Warni Textile'), ('PT Semesta Raya Abadi'),
('Toko Berlian Elektronik'), ('PT Garuda Prima Industri'), ('CV Mekar Sari Jaya');

DECLARE @catIds TABLE (rn INT IDENTITY(1,1), id INT);
INSERT INTO @catIds (id) SELECT id FROM master_customer_category;
DECLARE @catCount INT = (SELECT COUNT(*) FROM @catIds);

DECLARE @i INT = 1;
WHILE @i <= 30
BEGIN
    DECLARE @cname NVARCHAR(200) = (SELECT name FROM @CustomerNames WHERE n = @i);
    DECLARE @catId INT = (SELECT id FROM @catIds WHERE rn = ((@i - 1) % @catCount) + 1);

    INSERT INTO master_customer (customer_name, customer_code, no_telp_bisnis, alamat, email, category_id, is_active)
    VALUES (
        @cname,
        'DEMO-' + RIGHT('000' + CAST(@i AS VARCHAR), 3),
        '0812' + RIGHT('8000000' + CAST(1000000 + @i AS VARCHAR), 7),
        'Jl. Demo Raya No. ' + CAST(@i AS VARCHAR) + ', Jakarta',
        'contact' + CAST(@i AS VARCHAR) + '@demo-customer.co.id',
        @catId,
        1
    );

    SET @i = @i + 1;
END

-- ------------------------------------------------------------
-- 2. Seed 150 sales_order + sales_order_detail
-- ------------------------------------------------------------
DECLARE @demoCustomerIds TABLE (rn INT IDENTITY(1,1), customer_id INT);
INSERT INTO @demoCustomerIds (customer_id)
SELECT customer_id FROM master_customer WHERE customer_code LIKE 'DEMO-%';
DECLARE @custCount INT = (SELECT COUNT(*) FROM @demoCustomerIds);

DECLARE @demoProductIds TABLE (rn INT IDENTITY(1,1), product_id INT, uom_id INT);
INSERT INTO @demoProductIds (product_id, uom_id)
SELECT product_id, uom_id FROM master_product;
DECLARE @prodCount INT = (SELECT COUNT(*) FROM @demoProductIds);

IF @prodCount = 0
BEGIN
    RAISERROR('Tidak ada baris di master_product — seed produk dulu sebelum menjalankan script ini.', 16, 1);
END

DECLARE @soOrderIds TABLE (rn INT IDENTITY(1,1), order_id INT, so_date DATETIME, customer_id INT);

DECLARE @j INT = 1;
WHILE @j <= 150
BEGIN
    DECLARE @custId INT;
    SELECT TOP 1 @custId = customer_id FROM @demoCustomerIds ORDER BY NEWID();
    DECLARE @daysAgo INT = ABS(CHECKSUM(NEWID())) % 365;
    DECLARE @soDate DATETIME = DATEADD(DAY, -@daysAgo, GETDATE());
    DECLARE @kirimDate DATETIME = DATEADD(DAY, 3 + (ABS(CHECKSUM(NEWID())) % 7), @soDate);
    -- Status disesuaikan dengan 5 status Sales Order kanonis hasil redesain
    -- flow (lihat seed_sales_flow_accurate_demo_data.sql untuk versi yang
    -- relasinya konsisten -- script ini murni angka acak untuk uji dashboard).
    DECLARE @statusPick INT = ABS(CHECKSUM(NEWID())) % 10;
    DECLARE @status VARCHAR(30) = CASE @statusPick
        WHEN 0 THEN 'Completed'
        WHEN 1 THEN 'Completed'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Processing'
        WHEN 4 THEN 'Processing'
        WHEN 5 THEN 'In Delivery'
        WHEN 6 THEN 'Processing'
        WHEN 7 THEN 'Processing'
        WHEN 8 THEN 'Draft'
        ELSE 'Cancelled'
    END;

    INSERT INTO sales_order (so_number, tanggal_kirim, so_date, po_number, subtotal, customer_id, is_taxable, is_tax_included, address, notes, discount_total, tax_total, status)
    VALUES (
        'SO-DEMO-' + RIGHT('00000' + CAST(@j AS VARCHAR), 5),
        @kirimDate,
        @soDate,
        'PO-' + CAST(1000 + @j AS VARCHAR),
        0,
        @custId,
        1, 0,
        (SELECT alamat FROM master_customer WHERE customer_id = @custId),
        'Sample order (demo seed)',
        0, 0,
        @status
    );

    DECLARE @orderId INT = CAST(SCOPE_IDENTITY() AS INT);
    INSERT INTO @soOrderIds (order_id, so_date, customer_id) VALUES (@orderId, @soDate, @custId);

    DECLARE @lineCount INT = 1 + (ABS(CHECKSUM(NEWID())) % 3);
    DECLARE @orderTotal DECIMAL(18,2) = 0;
    DECLARE @k INT = 1;
    WHILE @k <= @lineCount
    BEGIN
        DECLARE @productId INT, @uomId INT;
        SELECT TOP 1 @productId = product_id, @uomId = uom_id FROM @demoProductIds ORDER BY NEWID();
        DECLARE @qty INT = 1 + (ABS(CHECKSUM(NEWID())) % 20);
        DECLARE @price DECIMAL(18,2) = 50000 + (ABS(CHECKSUM(NEWID())) % 4950000);
        DECLARE @discPct DECIMAL(5,2) = (ABS(CHECKSUM(NEWID())) % 10);
        DECLARE @lineTotal DECIMAL(18,2) = @qty * @price * (1 - @discPct / 100.0);

        INSERT INTO sales_order_detail (order_id, product_id, product_code, product_name, product_qty, product_price, discount_percent, total_price, warehouse_id, uom_id)
        SELECT @orderId, @productId, product_code, product_name, @qty, @price, @discPct, @lineTotal,
               (SELECT TOP 1 warehouse_id FROM master_warehouse ORDER BY NEWID()),
               @uomId
        FROM master_product WHERE product_id = @productId;

        SET @orderTotal = @orderTotal + @lineTotal;
        SET @k = @k + 1;
    END

    UPDATE sales_order SET subtotal = @orderTotal WHERE order_id = @orderId;

    SET @j = @j + 1;
END

-- ------------------------------------------------------------
-- 3. Seed 120 sales_invoice + sales_invoice_detail (link ke 120 dari 150 SO)
-- ------------------------------------------------------------
DECLARE @soForInvoice TABLE (rn INT IDENTITY(1,1), order_id INT, so_date DATETIME, customer_id INT);
INSERT INTO @soForInvoice (order_id, so_date, customer_id)
SELECT TOP 120 order_id, so_date, customer_id FROM @soOrderIds ORDER BY NEWID();

DECLARE @m INT = 1;
WHILE @m <= 120
BEGIN
    DECLARE @invOrderId INT = (SELECT order_id FROM @soForInvoice WHERE rn = @m);
    DECLARE @invSoDate DATETIME = (SELECT so_date FROM @soForInvoice WHERE rn = @m);
    DECLARE @invCustId INT = (SELECT customer_id FROM @soForInvoice WHERE rn = @m);
    DECLARE @invDate DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @invSoDate);
    DECLARE @dueDate DATETIME = DATEADD(DAY, 30, @invDate);
    DECLARE @invStatusPick INT = ABS(CHECKSUM(NEWID())) % 9;
    DECLARE @invStatus VARCHAR(30) = CASE @invStatusPick
        WHEN 0 THEN 'Paid'
        WHEN 1 THEN 'Paid'
        WHEN 2 THEN 'Paid'
        WHEN 3 THEN 'Partially Paid'
        WHEN 4 THEN 'Partially Paid'
        WHEN 5 THEN 'Issued'
        WHEN 6 THEN 'Issued'
        WHEN 7 THEN 'Overdue'
        ELSE 'Cancelled'
    END;

    INSERT INTO sales_invoice (invoice_number, customer_id, sales_order_id, delivery_order_id, invoice_date, due_date, status, subtotal, discount_total, tax_total, down_payment_amount, shipping_cost, grand_total, paid_amount, remaining_amount, notes, created_by, created_at, updated_at)
    VALUES (
        'INV-DEMO-' + RIGHT('00000' + CAST(@m AS VARCHAR), 5),
        @invCustId, @invOrderId, NULL,
        @invDate, @dueDate, @invStatus,
        0, 0, 0, 0, 0, 0, 0, 0,
        'Sample invoice (demo seed)', 'system-seed', GETDATE(), GETDATE()
    );

    DECLARE @invId INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO sales_invoice_detail (sales_invoice_id, product_id, description, quantity, uom_id, price, discount, tax, subtotal, warehouse_id, created_at, updated_at)
    SELECT @invId, sod.product_id, sod.product_name, sod.product_qty, sod.uom_id, sod.product_price,
           0,
           sod.total_price * 0.11,
           sod.total_price,
           sod.warehouse_id, GETDATE(), GETDATE()
    FROM sales_order_detail sod
    WHERE sod.order_id = @invOrderId;

    DECLARE @invSubtotal DECIMAL(18,2) = (SELECT SUM(subtotal) FROM sales_invoice_detail WHERE sales_invoice_id = @invId);
    DECLARE @invTax DECIMAL(18,2) = (SELECT SUM(tax) FROM sales_invoice_detail WHERE sales_invoice_id = @invId);
    DECLARE @invGrandTotal DECIMAL(18,2) = @invSubtotal + @invTax;

    DECLARE @paidAmt DECIMAL(18,2) = CASE @invStatus
        WHEN 'Paid' THEN @invGrandTotal
        WHEN 'Partially Paid' THEN @invGrandTotal * (0.3 + (ABS(CHECKSUM(NEWID())) % 40) / 100.0)
        ELSE 0
    END;
    DECLARE @remainingAmt DECIMAL(18,2) = @invGrandTotal - @paidAmt;

    UPDATE sales_invoice
    SET subtotal = @invSubtotal,
        tax_total = @invTax,
        grand_total = @invGrandTotal,
        paid_amount = @paidAmt,
        remaining_amount = @remainingAmt
    WHERE id = @invId;

    SET @m = @m + 1;
END

COMMIT TRANSACTION SeedSalesDemo;

DECLARE @cntCust INT = (SELECT COUNT(*) FROM master_customer WHERE customer_code LIKE 'DEMO-%');
DECLARE @cntSO INT = (SELECT COUNT(*) FROM sales_order WHERE so_number LIKE 'SO-DEMO-%');
DECLARE @cntInv INT = (SELECT COUNT(*) FROM sales_invoice WHERE invoice_number LIKE 'INV-DEMO-%');

PRINT 'Seed selesai.';
PRINT 'Customer demo : ' + CAST(@cntCust AS VARCHAR);
PRINT 'Sales Order   : ' + CAST(@cntSO AS VARCHAR);
PRINT 'Sales Invoice : ' + CAST(@cntInv AS VARCHAR);

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION SeedSalesDemo;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
