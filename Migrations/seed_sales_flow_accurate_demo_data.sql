-- ============================================================
-- SEED SCRIPT: Sample Sales Data — MENGIKUTI ALUR STATUS RESMI
-- (bukan status acak seperti seed_sales_demo_data.sql sebelumnya)
--
-- Aturan status yang diikuti (sesuai brief):
--   Quotation dibuat                          -> Draft
--   SO dibuat dari Quotation                  -> SO: Draft,        Quotation: Processed
--   Delivery Order dibuat dari SO             -> SO: In Delivery,  DO: Draft
--   Invoice dibuat dari Delivery Order        -> DO: Invoiced,     SO: Invoiced, Invoice: Issued
--   Sales Receipt (bayar penuh)               -> Invoice: Paid,          Receipt: Validated, SO: Completed
--   Sales Receipt (bayar sebagian)             -> Invoice: Partially Paid, Receipt: Validated, SO: Partially Paid
--       (SO Partially Paid untuk kasus bayar sebagian adalah interpretasi
--        saya berdasarkan logic cascade yang sudah dikonfirmasi di
--        SalesReceiptRepo.UpdateSalesOrderInvoiceStatus — beri tahu saya
--        kalau ternyata SO harus tetap Completed meski baru dibayar sebagian)
--
--   Alur Uang Muka (tanpa Delivery/Invoice):
--   SO dibuat (langsung, tanpa quotation)     -> SO: Draft
--   Uang Muka dibuat dari SO tsb              -> SO: Partially Paid, UangMuka: Draft (belum dibayar)
--   Sales Receipt dibuat dari Uang Muka tsb   -> UangMuka: Received
--
-- Semua data ditag 'FLOWDEMO-' / 'SO-FLOWDEMO-' / dst supaya gampang
-- dibedakan dari batch seed_sales_demo_data.sql sebelumnya dan gampang
-- dihapus lewat seed_sales_flow_accurate_demo_data_cleanup.sql.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION SeedFlowAccurate;

BEGIN TRY

-- ------------------------------------------------------------
-- 0. Guard — pastikan master data pendukung tersedia
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM master_customer_category)
BEGIN
    INSERT INTO master_customer_category (category_name, is_active) VALUES ('Umum', 1);
END

DECLARE @prodCount INT = (SELECT COUNT(*) FROM master_product);
IF @prodCount = 0
    RAISERROR('Tidak ada baris di master_product — seed produk dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @whCount INT = (SELECT COUNT(*) FROM master_warehouse);
IF @whCount = 0
    RAISERROR('Tidak ada baris di master_warehouse — seed gudang dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @bankCount INT = (SELECT COUNT(*) FROM bank);
IF @bankCount = 0
    RAISERROR('Tidak ada baris di tabel bank — seed data bank dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @delcatCount INT = (SELECT COUNT(*) FROM delivery_category);
IF @delcatCount = 0
    RAISERROR('Tidak ada baris di delivery_category — seed data itu dulu sebelum menjalankan script ini.', 16, 1);

-- ------------------------------------------------------------
-- 1. Seed 30 customer demo (prefix FLOWDEMO-)
-- ------------------------------------------------------------
DECLARE @CustomerNames TABLE (n INT IDENTITY(1,1), name NVARCHAR(200));
INSERT INTO @CustomerNames (name) VALUES
('PT Nusantara Sejahtera'), ('CV Bumi Persada'), ('PT Cipta Selaras'),
('Toko Rejeki Abadi'), ('PT Wira Utama Sentosa'), ('CV Sinergi Mandiri'),
('PT Karya Gemilang'), ('Toko Sumber Makmur'), ('PT Andalan Prima Jaya'),
('CV Cakra Buana'), ('PT Mitra Sejati Abadi'), ('Toko Berkah Sentosa'),
('PT Graha Cendekia'), ('CV Lintas Nusa'), ('PT Persada Unggul'),
('Toko Cahaya Timur'), ('PT Bina Karya Mandiri'), ('CV Anugerah Prima'),
('PT Sentra Logistik Jaya'), ('Toko Damai Sejahtera'), ('PT Rimba Raya Sentosa'),
('CV Pelangi Nusantara'), ('PT Duta Sarana Abadi'), ('Toko Harmoni Jaya'),
('PT Cendana Emas'), ('CV Baraka Sejahtera'), ('PT Elang Perkasa'),
('Toko Sido Makmur'), ('PT Indah Cemerlang'), ('CV Wahana Jaya Sentosa');

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
        'FLOWDEMO-' + RIGHT('000' + CAST(@i AS VARCHAR), 3),
        '0813' + RIGHT('7000000' + CAST(2000000 + @i AS VARCHAR), 7),
        'Jl. Flow Demo No. ' + CAST(@i AS VARCHAR) + ', Bandung',
        'flowdemo' + CAST(@i AS VARCHAR) + '@sample-customer.co.id',
        @catId,
        1
    );

    SET @i = @i + 1;
END

DECLARE @demoCustomerIds TABLE (rn INT IDENTITY(1,1), customer_id INT);
INSERT INTO @demoCustomerIds (customer_id)
SELECT customer_id FROM master_customer WHERE customer_code LIKE 'FLOWDEMO-%';

DECLARE @demoProductIds TABLE (rn INT IDENTITY(1,1), product_id INT, uom_id INT);
INSERT INTO @demoProductIds (product_id, uom_id)
SELECT product_id, uom_id FROM master_product;

-- ------------------------------------------------------------
-- 2. Loop A — 30 Quotation berdiri sendiri (TIDAK jadi SO)
--    Status tetap Draft (sesuai aturan: baru dibuat = Draft)
-- ------------------------------------------------------------
DECLARE @a INT = 1;
WHILE @a <= 30
BEGIN
    DECLARE @custIdA INT;
    SELECT TOP 1 @custIdA = customer_id FROM @demoCustomerIds ORDER BY NEWID();

    DECLARE @qDateA DATETIME = DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 240), GETDATE());

    INSERT INTO sales_quotation (customer_id, quotation_number, quotation_date, address, notes, is_taxable, is_tax_included, subtotal, discount_total, tax_total, status)
    VALUES (
        @custIdA,
        'SQ-FLOWDEMO-' + RIGHT('00000' + CAST(@a AS VARCHAR), 5),
        @qDateA,
        (SELECT alamat FROM master_customer WHERE customer_id = @custIdA),
        'Sample quotation only (flow-accurate demo)',
        1, 0, 0, 0, 0,
        'Draft'
    );

    DECLARE @qIdA INT = CAST(SCOPE_IDENTITY() AS INT);

    DECLARE @lineCountA INT = 1 + (ABS(CHECKSUM(NEWID())) % 3);
    DECLARE @kA INT = 1;
    DECLARE @qTotalA DECIMAL(18,2) = 0;
    WHILE @kA <= @lineCountA
    BEGIN
        DECLARE @productIdA INT, @uomIdA INT;
        SELECT TOP 1 @productIdA = product_id, @uomIdA = uom_id FROM @demoProductIds ORDER BY NEWID();
        DECLARE @qtyA INT = 1 + (ABS(CHECKSUM(NEWID())) % 15);
        DECLARE @priceA DECIMAL(18,2) = 50000 + (ABS(CHECKSUM(NEWID())) % 4950000);
        DECLARE @discA DECIMAL(5,2) = (ABS(CHECKSUM(NEWID())) % 10);

        INSERT INTO quotation_detail (quotation_id, product_id, quantity, uom_id, price, discount_percent)
        VALUES (@qIdA, @productIdA, @qtyA, @uomIdA, @priceA, @discA);

        SET @qTotalA = @qTotalA + (@qtyA * @priceA * (1 - @discA / 100.0));
        SET @kA = @kA + 1;
    END

    UPDATE sales_quotation SET subtotal = @qTotalA WHERE quotation_id = @qIdA;

    SET @a = @a + 1;
END

-- ------------------------------------------------------------
-- 3. Loop B — 150 Quotation -> SO, bertahap ke DO/Invoice/Receipt
--    sesuai bucket (rentang iterasi @b):
--      1-20    : Bucket B — SO Draft saja
--      21-45   : Bucket C — + Delivery Order (SO: In Delivery, DO: Draft)
--      46-75   : Bucket D — + Invoice (DO: Invoiced, SO: Invoiced, Invoice: Issued)
--      76-120  : Bucket E — + Sales Receipt LUNAS (Invoice: Paid, SO: Completed)
--      121-150 : Bucket F — + Sales Receipt SEBAGIAN (Invoice: Partially Paid, SO: Partially Paid)
-- ------------------------------------------------------------
DECLARE @b INT = 1;
WHILE @b <= 150
BEGIN
    DECLARE @custIdB INT;
    SELECT TOP 1 @custIdB = customer_id FROM @demoCustomerIds ORDER BY NEWID();
    DECLARE @custAddrB NVARCHAR(500) = (SELECT alamat FROM master_customer WHERE customer_id = @custIdB);

    DECLARE @qDateB DATETIME = DATEADD(DAY, -(240 - (@b * 240 / 150)), GETDATE());

    -- 3a. Quotation (jadi Processed karena akan dikonversi ke SO)
    INSERT INTO sales_quotation (customer_id, quotation_number, quotation_date, address, notes, is_taxable, is_tax_included, subtotal, discount_total, tax_total, status)
    VALUES (
        @custIdB,
        'SQ-FLOWDEMO-' + RIGHT('00000' + CAST(1000 + @b AS VARCHAR), 5),
        @qDateB,
        @custAddrB,
        'Sample quotation -> SO chain (flow-accurate demo)',
        1, 0, 0, 0, 0,
        'Processed'
    );
    DECLARE @qIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    -- 3b. Sales Order dari quotation ini
    DECLARE @soDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 3), @qDateB);
    DECLARE @kirimDateB DATETIME = DATEADD(DAY, 3 + (ABS(CHECKSUM(NEWID())) % 5), @soDateB);

    INSERT INTO sales_order (so_number, tanggal_kirim, so_date, po_number, subtotal, customer_id, is_taxable, is_tax_included, address, notes, discount_total, tax_total, status, quotation_id)
    VALUES (
        'SO-FLOWDEMO-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @kirimDateB, @soDateB,
        'PO-FD-' + CAST(1000 + @b AS VARCHAR),
        0,
        @custIdB, 1, 0,
        @custAddrB,
        'Sample order from quotation (flow-accurate demo)',
        0, 0,
        'Draft',
        @qIdB
    );
    DECLARE @orderIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    -- 3c. Detail item — dipakai untuk quotation, SO, dan (nanti) invoice
    DECLARE @lineCountB INT = 1 + (ABS(CHECKSUM(NEWID())) % 3);
    DECLARE @kB INT = 1;
    DECLARE @soTotalB DECIMAL(18,2) = 0;
    WHILE @kB <= @lineCountB
    BEGIN
        DECLARE @productIdB INT, @uomIdB INT;
        SELECT TOP 1 @productIdB = product_id, @uomIdB = uom_id FROM @demoProductIds ORDER BY NEWID();
        DECLARE @qtyB INT = 1 + (ABS(CHECKSUM(NEWID())) % 15);
        DECLARE @priceB DECIMAL(18,2) = 50000 + (ABS(CHECKSUM(NEWID())) % 4950000);
        DECLARE @discB DECIMAL(5,2) = (ABS(CHECKSUM(NEWID())) % 10);
        DECLARE @lineTotalB DECIMAL(18,2) = @qtyB * @priceB * (1 - @discB / 100.0);
        DECLARE @whIdB INT;
        SELECT TOP 1 @whIdB = warehouse_id FROM master_warehouse ORDER BY NEWID();

        INSERT INTO quotation_detail (quotation_id, product_id, quantity, uom_id, price, discount_percent)
        VALUES (@qIdB, @productIdB, @qtyB, @uomIdB, @priceB, @discB);

        INSERT INTO sales_order_detail (order_id, product_id, product_code, product_name, product_qty, product_price, discount_percent, total_price, warehouse_id, uom_id)
        SELECT @orderIdB, @productIdB, product_code, product_name, @qtyB, @priceB, @discB, @lineTotalB, @whIdB, @uomIdB
        FROM master_product WHERE product_id = @productIdB;

        SET @soTotalB = @soTotalB + @lineTotalB;
        SET @kB = @kB + 1;
    END

    UPDATE sales_quotation SET subtotal = @soTotalB WHERE quotation_id = @qIdB;
    UPDATE sales_order SET subtotal = @soTotalB WHERE order_id = @orderIdB;

    -- ── Bucket B (iter 1-20): berhenti di SO Draft ────────────────────
    IF @b <= 20
    BEGIN
        -- status SO tetap 'Draft', tidak ada aksi lanjutan
        SET @b = @b + 1;
        CONTINUE;
    END

    -- ── Bucket C-F (iter 21+): buat Delivery Order ────────────────────
    UPDATE sales_order SET status = 'In Delivery' WHERE order_id = @orderIdB;

    DECLARE @doDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 4), @kirimDateB);
    DECLARE @delCatIdB INT;
    SELECT TOP 1 @delCatIdB = id FROM delivery_category ORDER BY NEWID();

    INSERT INTO delivery_order_header (customer_id, do_number, delivery_category_id, po_number, address, notes, do_date, so_id, status)
    VALUES (
        @custIdB,
        'SJ-FLOWDEMO-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @delCatIdB,
        'PO-FD-' + CAST(1000 + @b AS VARCHAR),
        @custAddrB,
        'Sample delivery from SO (flow-accurate demo)',
        @doDateB,
        @orderIdB,
        'Draft'
    );
    DECLARE @doIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
    SELECT @doIdB, sod.product_id, sod.product_qty, sod.product_qty, sod.warehouse_id
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdB;

    -- ── Bucket C (iter 21-45): berhenti di Delivery Order Draft ───────
    IF @b <= 45
    BEGIN
        SET @b = @b + 1;
        CONTINUE;
    END

    -- ── Bucket D-F (iter 46+): buat Invoice dari Delivery Order ───────
    UPDATE delivery_order_header SET status = 'Invoiced' WHERE id = @doIdB;
    UPDATE sales_order SET status = 'Invoiced' WHERE order_id = @orderIdB;

    DECLARE @invDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 4), @doDateB);
    DECLARE @dueDateB DATETIME = DATEADD(DAY, 30, @invDateB);
    DECLARE @invTaxB DECIMAL(18,2) = @soTotalB * 0.11;
    DECLARE @invGrandB DECIMAL(18,2) = @soTotalB + @invTaxB;

    INSERT INTO sales_invoice (invoice_number, customer_id, sales_order_id, delivery_order_id, invoice_date, due_date, status, subtotal, discount_total, tax_total, down_payment_amount, shipping_cost, grand_total, paid_amount, remaining_amount, notes, created_by, created_at, updated_at)
    VALUES (
        'INV-FLOWDEMO-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @custIdB, @orderIdB, @doIdB,
        @invDateB, @dueDateB, 'Issued',
        @soTotalB, 0, @invTaxB, 0, 0, @invGrandB, 0, @invGrandB,
        'Sample invoice from delivery (flow-accurate demo)', 'system-seed', GETDATE(), GETDATE()
    );
    DECLARE @invIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO sales_invoice_detail (sales_invoice_id, product_id, description, quantity, uom_id, price, discount, tax, subtotal, warehouse_id, created_at, updated_at)
    SELECT @invIdB, sod.product_id, sod.product_name, sod.product_qty, sod.uom_id, sod.product_price,
           0, sod.total_price * 0.11, sod.total_price, sod.warehouse_id, GETDATE(), GETDATE()
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdB;

    -- ── Bucket D (iter 46-75): berhenti di Invoice Issued, belum dibayar
    IF @b <= 75
    BEGIN
        SET @b = @b + 1;
        CONTINUE;
    END

    -- ── Bucket E-F (iter 76+): buat Sales Receipt ─────────────────────
    DECLARE @bankIdB INT;
    SELECT TOP 1 @bankIdB = id FROM bank ORDER BY NEWID();
    DECLARE @receiptDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 10), @invDateB);

    IF @b <= 120
    BEGIN
        -- ── Bucket E (iter 76-120): LUNAS ──────────────────────────────
        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
        VALUES (
            'BP-FLOWDEMO-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
            @custIdB, @bankIdB, @invGrandB, @receiptDateB,
            @orderIdB, @invIdB, 'Validated'
        );

        UPDATE sales_invoice SET status = 'Paid', paid_amount = @invGrandB, remaining_amount = 0 WHERE id = @invIdB;
        UPDATE sales_order SET status = 'Completed' WHERE order_id = @orderIdB;
    END
    ELSE
    BEGIN
        -- ── Bucket F (iter 121-150): SEBAGIAN (30%-70%) ────────────────
        DECLARE @paidPctB DECIMAL(5,4) = 0.3 + ((ABS(CHECKSUM(NEWID())) % 40) / 100.0);
        DECLARE @paidAmtB DECIMAL(18,2) = ROUND(@invGrandB * @paidPctB, 0);
        DECLARE @remainAmtB DECIMAL(18,2) = @invGrandB - @paidAmtB;

        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
        VALUES (
            'BP-FLOWDEMO-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
            @custIdB, @bankIdB, @paidAmtB, @receiptDateB,
            @orderIdB, @invIdB, 'Validated'
        );

        UPDATE sales_invoice SET status = 'Partially Paid', paid_amount = @paidAmtB, remaining_amount = @remainAmtB WHERE id = @invIdB;
        UPDATE sales_order SET status = 'Partially Paid' WHERE order_id = @orderIdB;
    END

    SET @b = @b + 1;
END

-- ------------------------------------------------------------
-- 4. Loop C — 20 SO langsung (TANPA quotation) -> Uang Muka -> (sebagian) Receipt
--      Semua 20  : SO Draft -> Uang Muka dibuat -> SO jadi Partially Paid
--      15 dari 20: Uang Muka juga dapat Sales Receipt -> UangMuka jadi Received
--      5 sisanya : Uang Muka tetap Draft (belum dibayar)
-- ------------------------------------------------------------
DECLARE @c INT = 1;
WHILE @c <= 20
BEGIN
    DECLARE @custIdC INT;
    SELECT TOP 1 @custIdC = customer_id FROM @demoCustomerIds ORDER BY NEWID();
    DECLARE @custAddrC NVARCHAR(500) = (SELECT alamat FROM master_customer WHERE customer_id = @custIdC);

    DECLARE @soDateC DATETIME = DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 240), GETDATE());
    DECLARE @kirimDateC DATETIME = DATEADD(DAY, 5 + (ABS(CHECKSUM(NEWID())) % 5), @soDateC);

    INSERT INTO sales_order (so_number, tanggal_kirim, so_date, po_number, subtotal, customer_id, is_taxable, is_tax_included, address, notes, discount_total, tax_total, status, quotation_id)
    VALUES (
        'SO-FLOWDEMO-' + RIGHT('00000' + CAST(2000 + @c AS VARCHAR), 5),
        @kirimDateC, @soDateC,
        'PO-FD-' + CAST(2000 + @c AS VARCHAR),
        0,
        @custIdC, 1, 0,
        @custAddrC,
        'Sample direct order for down-payment flow (flow-accurate demo)',
        0, 0,
        'Draft',
        NULL
    );
    DECLARE @orderIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    DECLARE @lineCountC INT = 1 + (ABS(CHECKSUM(NEWID())) % 3);
    DECLARE @kC INT = 1;
    DECLARE @soTotalC DECIMAL(18,2) = 0;
    WHILE @kC <= @lineCountC
    BEGIN
        DECLARE @productIdC INT, @uomIdC INT;
        SELECT TOP 1 @productIdC = product_id, @uomIdC = uom_id FROM @demoProductIds ORDER BY NEWID();
        DECLARE @qtyC INT = 1 + (ABS(CHECKSUM(NEWID())) % 15);
        DECLARE @priceC DECIMAL(18,2) = 50000 + (ABS(CHECKSUM(NEWID())) % 4950000);
        DECLARE @discC DECIMAL(5,2) = (ABS(CHECKSUM(NEWID())) % 10);
        DECLARE @lineTotalC DECIMAL(18,2) = @qtyC * @priceC * (1 - @discC / 100.0);
        DECLARE @whIdC INT;
        SELECT TOP 1 @whIdC = warehouse_id FROM master_warehouse ORDER BY NEWID();

        INSERT INTO sales_order_detail (order_id, product_id, product_code, product_name, product_qty, product_price, discount_percent, total_price, warehouse_id, uom_id)
        SELECT @orderIdC, @productIdC, product_code, product_name, @qtyC, @priceC, @discC, @lineTotalC, @whIdC, @uomIdC
        FROM master_product WHERE product_id = @productIdC;

        SET @soTotalC = @soTotalC + @lineTotalC;
        SET @kC = @kC + 1;
    END

    UPDATE sales_order SET subtotal = @soTotalC WHERE order_id = @orderIdC;

    -- Uang Muka dari SO ini — SO langsung jadi Partially Paid begitu DP dibuat
    DECLARE @soNumberC VARCHAR(50) = (SELECT so_number FROM sales_order WHERE order_id = @orderIdC);
    DECLARE @dpDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @soDateC);
    DECLARE @dpAmountC DECIMAL(18,2) = ROUND(@soTotalC * (0.2 + (ABS(CHECKSUM(NEWID())) % 30) / 100.0), 0);

    INSERT INTO uang_muka (NoFaktur, Tanggal, CustomerId, NoPO, NoSo, NominalUangMuka, IsTaxable, IsTaxIncluded, TaxAmount, TotalAmount, SyaratPembayaran, Alamat, Keterangan, Status, CreatedAt, UpdatedAt, CreatedBy)
    VALUES (
        'UM-FLOWDEMO-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @dpDateC, @custIdC,
        'PO-FD-' + CAST(2000 + @c AS VARCHAR),
        @soNumberC,
        @dpAmountC, 0, 0, 0, @dpAmountC,
        'Net 30',
        @custAddrC,
        'Sample down payment (flow-accurate demo)',
        'Draft',
        GETDATE(), GETDATE(), 'system-seed'
    );
    DECLARE @umIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    UPDATE sales_order SET status = 'Partially Paid' WHERE order_id = @orderIdC;

    -- 15 dari 20 dapat Sales Receipt -> Uang Muka jadi Received
    IF @c <= 15
    BEGIN
        DECLARE @bankIdC INT;
        SELECT TOP 1 @bankIdC = id FROM bank ORDER BY NEWID();
        DECLARE @receiptDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @dpDateC);

        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, uang_muka_id, sales_order_id, status)
        VALUES (
            'BP-FLOWDEMO-DP-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
            @custIdC, @bankIdC, @dpAmountC, @receiptDateC,
            @umIdC, @orderIdC, 'Validated'
        );

        UPDATE uang_muka SET Status = 'Received', UpdatedAt = GETDATE() WHERE Id = @umIdC;
    END

    SET @c = @c + 1;
END

COMMIT TRANSACTION SeedFlowAccurate;

DECLARE @cntCust INT = (SELECT COUNT(*) FROM master_customer WHERE customer_code LIKE 'FLOWDEMO-%');
DECLARE @cntQuot INT = (SELECT COUNT(*) FROM sales_quotation WHERE quotation_number LIKE 'SQ-FLOWDEMO-%');
DECLARE @cntSO INT = (SELECT COUNT(*) FROM sales_order WHERE so_number LIKE 'SO-FLOWDEMO-%');
DECLARE @cntDO INT = (SELECT COUNT(*) FROM delivery_order_header WHERE do_number LIKE 'SJ-FLOWDEMO-%');
DECLARE @cntInv INT = (SELECT COUNT(*) FROM sales_invoice WHERE invoice_number LIKE 'INV-FLOWDEMO-%');
DECLARE @cntReceipt INT = (SELECT COUNT(*) FROM sales_receipt WHERE no_bukti LIKE 'BP-FLOWDEMO-%');
DECLARE @cntUM INT = (SELECT COUNT(*) FROM uang_muka WHERE NoFaktur LIKE 'UM-FLOWDEMO-%');

PRINT 'Seed selesai.';
PRINT 'Customer     : ' + CAST(@cntCust AS VARCHAR);
PRINT 'Quotation    : ' + CAST(@cntQuot AS VARCHAR) + ' (30 berdiri sendiri + 150 jadi SO)';
PRINT 'Sales Order  : ' + CAST(@cntSO AS VARCHAR) + ' (150 dari quotation + 20 langsung/DP-flow)';
PRINT 'Delivery Order: ' + CAST(@cntDO AS VARCHAR);
PRINT 'Invoice      : ' + CAST(@cntInv AS VARCHAR);
PRINT 'Sales Receipt: ' + CAST(@cntReceipt AS VARCHAR);
PRINT 'Uang Muka    : ' + CAST(@cntUM AS VARCHAR);

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION SeedFlowAccurate;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
