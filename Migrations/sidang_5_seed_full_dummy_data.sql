-- ============================================================
-- LANGKAH 4/4b — Data dummy LENGKAP untuk seluruh Sales module:
-- Customer, Customer Category, Quotation, Sales Order (reguler +
-- indent), Uang Muka, Invoice (reguler + Proforma DP/Pelunasan),
-- Sales Receipt, Delivery Order -- mengikuti persis alur status resmi
-- yang berlaku di aplikasi saat ini:
--
--   Quotation dibuat                          -> Draft
--   SO dibuat dari Quotation                  -> SO: Draft, Quotation: Processed
--   Invoice pertama dibuat untuk SO tsb        -> SO: Processing, Invoice: Issued
--   Sales Receipt (bayar sebagian)             -> Invoice: Partially Paid  (SO tetap Processing)
--   Sales Receipt (bayar penuh, SEMUA invoice
--     terkait SO sudah lunas)                  -> Invoice: Paid
--   Delivery Order dibuat (HANYA kalau seluruh
--     invoice SO sudah lunas 100%)             -> DO: In Delivery, SO: In Delivery
--   DO ditandai "Diterima", qty kirim = qty pesan (semua item)
--                                               -> DO: Received, SO: Completed
--   DO ditandai "Diterima", qty kirim < qty pesan (ada item kurang)
--                                               -> DO: Received, SO: Partially Fulfilled
--
--   Alur Barang Indent (sales_order.is_indent = 1, tanpa quotation):
--   SO dibuat langsung                         -> SO: Draft, is_indent = 1
--   Uang Muka dibuat (nominal = 30% dari SO)   -> UangMuka: Draft
--   Invoice Proforma DP dibuat dari SO          -> SO: Processing, Invoice DP: Issued
--   Sales Receipt atas Invoice DP (lunas)      -> Invoice DP: Paid, UangMuka: Received
--   Invoice Proforma Pelunasan dibuat           -> Invoice Final: Issued
--   Sales Receipt atas Invoice Final (lunas)   -> Invoice Final: Paid
--   Delivery Order dibuat (setelah KEDUA
--     invoice proforma lunas)                  -> DO: In Delivery, SO: In Delivery
--   DO ditandai "Diterima"                      -> DO: Received, SO: Completed/Partially Fulfilled
--
-- Semua data ditag prefix 'SIDANG-' / 'SO-SIDANG-' / dst supaya gampang
-- dibedakan dan dihapus lagi lewat sidang_cleanup_dummy_data.sql.
--
-- PRASYARAT: master_product, master_warehouse, bank, delivery_category
-- sudah terisi (data master modul lain, tidak dibuat ulang di sini).
-- Customer dibuat otomatis kalau belum ada (idempotent, sama seperti
-- sidang_4_seed_customers_only.sql -- aman jalankan salah satu / keduanya).
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION SeedSidangFull;

BEGIN TRY

-- ------------------------------------------------------------
-- 0. Guard -- pastikan master data pendukung tersedia
-- ------------------------------------------------------------
DECLARE @prodCount INT = (SELECT COUNT(*) FROM master_product);
IF @prodCount = 0
    RAISERROR('Tidak ada baris di master_product -- seed produk dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @whCount INT = (SELECT COUNT(*) FROM master_warehouse);
IF @whCount = 0
    RAISERROR('Tidak ada baris di master_warehouse -- seed gudang dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @bankCount INT = (SELECT COUNT(*) FROM bank);
IF @bankCount = 0
    RAISERROR('Tidak ada baris di tabel bank -- seed data bank dulu sebelum menjalankan script ini.', 16, 1);

DECLARE @delcatCount INT = (SELECT COUNT(*) FROM delivery_category);
IF @delcatCount = 0
    RAISERROR('Tidak ada baris di delivery_category -- seed data itu dulu sebelum menjalankan script ini.', 16, 1);

-- ------------------------------------------------------------
-- 1. Customer Category + 25 Customer (sama persis dengan
--    sidang_4_seed_customers_only.sql, idempotent -- kalau sudah
--    dibuat file itu duluan, blok ini di-skip otomatis).
-- ------------------------------------------------------------
DECLARE @Categories TABLE (n INT IDENTITY(1,1), name NVARCHAR(200));
INSERT INTO @Categories (name) VALUES
    ('Retail'), ('Distributor'), ('Korporat'), ('Kontraktor'), ('UMKM');

DECLARE @ci INT = 1;
WHILE @ci <= (SELECT COUNT(*) FROM @Categories)
BEGIN
    DECLARE @catName NVARCHAR(200) = (SELECT name FROM @Categories WHERE n = @ci);
    IF NOT EXISTS (SELECT 1 FROM master_customer_category WHERE category_name = @catName)
    BEGIN
        INSERT INTO master_customer_category (category_name, is_active) VALUES (@catName, 1);
    END
    SET @ci = @ci + 1;
END

DECLARE @CustomerNames TABLE (n INT IDENTITY(1,1), name NVARCHAR(200));
INSERT INTO @CustomerNames (name) VALUES
    ('PT Jaya Abadi'), ('CV Sumber Makmur'), ('PT Mitra Sentosa'),
    ('Toko Elektronik Merdeka'), ('PT Cahaya Nusantara Teknik'), ('CV Berkah Sejahtera'),
    ('PT Prima Logistik Indonesia'), ('Toko Bangunan Sido Mulyo'), ('PT Karya Utama Industri'),
    ('CV Anugerah Teknik'), ('PT Bintang Timur Perkasa'), ('Toko Sentosa Elektrik'),
    ('PT Maju Bersama'), ('CV Harapan Sejati'), ('PT Indo Perkasa'),
    ('Toko Cendana Bangunan'), ('PT Wahana Sukses'), ('CV Delta Prima'),
    ('PT Nusantara Bahari Jaya'), ('Toko Barokah Elektronik'), ('PT Kencana Mulia Abadi'),
    ('CV Trijaya Sentosa'), ('PT Pratama Sukses Mandiri'), ('Toko Alam Jaya'),
    ('PT Cipta Karya Sentosa');

DECLARE @catIds TABLE (rn INT IDENTITY(1,1), id INT);
INSERT INTO @catIds (id) SELECT id FROM master_customer_category ORDER BY id;
DECLARE @catCount INT = (SELECT COUNT(*) FROM @catIds);

DECLARE @i INT = 1;
WHILE @i <= (SELECT COUNT(*) FROM @CustomerNames)
BEGIN
    DECLARE @cname NVARCHAR(200) = (SELECT name FROM @CustomerNames WHERE n = @i);
    DECLARE @catId INT = (SELECT id FROM @catIds WHERE rn = ((@i - 1) % @catCount) + 1);

    IF NOT EXISTS (SELECT 1 FROM master_customer WHERE customer_code = 'SIDANG-' + RIGHT('000' + CAST(@i AS VARCHAR), 3))
    BEGIN
        INSERT INTO master_customer (customer_name, customer_code, no_telp_bisnis, alamat, email, category_id, is_active)
        VALUES (
            @cname,
            'SIDANG-' + RIGHT('000' + CAST(@i AS VARCHAR), 3),
            '0821' + RIGHT('5000000' + CAST(3000000 + @i AS VARCHAR), 7),
            'Jl. Contoh Raya No. ' + CAST(@i AS VARCHAR) + ', Jakarta',
            'kontak' + CAST(@i AS VARCHAR) + '@customer-sidang.co.id',
            @catId,
            1
        );
    END

    SET @i = @i + 1;
END

DECLARE @demoCustomerIds TABLE (rn INT IDENTITY(1,1), customer_id INT);
INSERT INTO @demoCustomerIds (customer_id)
SELECT customer_id FROM master_customer WHERE customer_code LIKE 'SIDANG-%';

DECLARE @demoProductIds TABLE (rn INT IDENTITY(1,1), product_id INT, uom_id INT);
INSERT INTO @demoProductIds (product_id, uom_id)
SELECT product_id, uom_id FROM master_product;

-- ------------------------------------------------------------
-- 2. Loop A -- 20 Quotation berdiri sendiri (TIDAK jadi SO), Draft
-- ------------------------------------------------------------
DECLARE @a INT = 1;
WHILE @a <= 20
BEGIN
    DECLARE @custIdA INT;
    SELECT TOP 1 @custIdA = customer_id FROM @demoCustomerIds ORDER BY NEWID();

    DECLARE @qDateA DATETIME = DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 240), GETDATE());

    INSERT INTO sales_quotation (customer_id, quotation_number, quotation_date, address, notes, is_taxable, is_tax_included, subtotal, discount_total, tax_total, status)
    VALUES (
        @custIdA,
        'SQ-SIDANG-' + RIGHT('00000' + CAST(@a AS VARCHAR), 5),
        @qDateA,
        (SELECT alamat FROM master_customer WHERE customer_id = @custIdA),
        'Sample quotation only (sidang demo)',
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
-- 3. Loop B -- 100 Quotation -> SO (non-indent), bertahap ke
--    Invoice/Pembayaran/DO sesuai bucket (rentang iterasi @b):
--      1-20   : Bucket B -- SO Draft saja (belum ada invoice)
--      21-45  : Bucket C -- + Invoice Issued (belum dibayar). SO: Processing
--      46-65  : Bucket D -- + Invoice Partially Paid.          SO: Processing
--      66-85  : Bucket E -- Invoice Paid LUNAS -> Delivery Order dibuat. SO: In Delivery
--      86-92  : Bucket F -- DO diterima PENUH (qty kirim = qty pesan). SO: Completed
--      93-100 : Bucket G -- DO diterima SEBAGIAN (qty kirim < qty pesan). SO: Partially Fulfilled
-- ------------------------------------------------------------
DECLARE @b INT = 1;
WHILE @b <= 100
BEGIN
    DECLARE @custIdB INT;
    SELECT TOP 1 @custIdB = customer_id FROM @demoCustomerIds ORDER BY NEWID();
    DECLARE @custAddrB NVARCHAR(500) = (SELECT alamat FROM master_customer WHERE customer_id = @custIdB);

    DECLARE @qDateB DATETIME = DATEADD(DAY, -(240 - (@b * 240 / 100)), GETDATE());

    -- 3a. Quotation (jadi Processed karena akan dikonversi ke SO)
    INSERT INTO sales_quotation (customer_id, quotation_number, quotation_date, address, notes, is_taxable, is_tax_included, subtotal, discount_total, tax_total, status)
    VALUES (
        @custIdB,
        'SQ-SIDANG-' + RIGHT('00000' + CAST(1000 + @b AS VARCHAR), 5),
        @qDateB,
        @custAddrB,
        'Sample quotation -> SO chain (sidang demo)',
        1, 0, 0, 0, 0,
        'Processed'
    );
    DECLARE @qIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    -- 3b. Sales Order dari quotation ini -- status awal "Draft"
    DECLARE @soDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 3), @qDateB);
    DECLARE @kirimDateB DATETIME = DATEADD(DAY, 3 + (ABS(CHECKSUM(NEWID())) % 5), @soDateB);

    INSERT INTO sales_order (so_number, tanggal_kirim, so_date, po_number, subtotal, customer_id, is_taxable, is_tax_included, address, notes, discount_total, tax_total, status, quotation_id, is_indent)
    VALUES (
        'SO-SIDANG-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @kirimDateB, @soDateB,
        'PO-SD-' + CAST(1000 + @b AS VARCHAR),
        0,
        @custIdB, 1, 0,
        @custAddrB,
        'Sample order from quotation (sidang demo)',
        0, 0,
        'Draft',
        @qIdB,
        0
    );
    DECLARE @orderIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    -- 3c. Detail item -- dipakai untuk quotation, SO, dan (nanti) invoice
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

    -- Bucket B (iter 1-20): berhenti di SO Draft
    IF @b <= 20
    BEGIN
        SET @b = @b + 1;
        CONTINUE;
    END

    -- Bucket C-G (iter 21+): buat Invoice reguler dari SO
    UPDATE sales_order SET status = 'Processing' WHERE order_id = @orderIdB;

    DECLARE @invDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 4), @soDateB);
    DECLARE @dueDateB DATETIME = DATEADD(DAY, 30, @invDateB);
    DECLARE @invTaxB DECIMAL(18,2) = @soTotalB * 0.11;
    DECLARE @invGrandB DECIMAL(18,2) = @soTotalB + @invTaxB;

    INSERT INTO sales_invoice (invoice_number, customer_id, sales_order_id, delivery_order_id, invoice_date, due_date, status, subtotal, discount_total, tax_total, down_payment_amount, shipping_cost, grand_total, paid_amount, remaining_amount, notes, created_by, created_at, updated_at, proforma_stage)
    VALUES (
        'INV-SIDANG-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @custIdB, @orderIdB, NULL,
        @invDateB, @dueDateB, 'Issued',
        @soTotalB, 0, @invTaxB, 0, 0, @invGrandB, 0, @invGrandB,
        'Sample invoice from Sales Order (sidang demo)', 'system-seed', GETDATE(), GETDATE(),
        NULL
    );
    DECLARE @invIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO sales_invoice_detail (sales_invoice_id, product_id, description, quantity, uom_id, price, discount, tax, subtotal, warehouse_id, created_at, updated_at)
    SELECT @invIdB, sod.product_id, sod.product_name, sod.product_qty, sod.uom_id, sod.product_price,
           0, sod.total_price * 0.11, sod.total_price, sod.warehouse_id, GETDATE(), GETDATE()
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdB;

    -- Bucket C (iter 21-45): berhenti di Invoice Issued, belum dibayar
    IF @b <= 45
    BEGIN
        SET @b = @b + 1;
        CONTINUE;
    END

    -- Bucket D-G (iter 46+): buat Sales Receipt
    DECLARE @bankIdB INT;
    SELECT TOP 1 @bankIdB = id FROM bank ORDER BY NEWID();
    DECLARE @receiptDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 10), @invDateB);

    IF @b <= 65
    BEGIN
        -- Bucket D (iter 46-65): SEBAGIAN (30%-70%)
        DECLARE @paidPctB DECIMAL(5,4) = 0.3 + ((ABS(CHECKSUM(NEWID())) % 40) / 100.0);
        DECLARE @paidAmtB DECIMAL(18,2) = ROUND(@invGrandB * @paidPctB, 0);
        DECLARE @remainAmtB DECIMAL(18,2) = @invGrandB - @paidAmtB;

        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
        VALUES (
            'BP-SIDANG-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
            @custIdB, @bankIdB, @paidAmtB, @receiptDateB,
            @orderIdB, @invIdB, 'Validated'
        );

        UPDATE sales_invoice SET status = 'Partially Paid', paid_amount = @paidAmtB, remaining_amount = @remainAmtB WHERE id = @invIdB;

        SET @b = @b + 1;
        CONTINUE;
    END

    -- Bucket E-G (iter 66+): LUNAS -> Delivery Order boleh dibuat
    INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
    VALUES (
        'BP-SIDANG-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @custIdB, @bankIdB, @invGrandB, @receiptDateB,
        @orderIdB, @invIdB, 'Validated'
    );

    UPDATE sales_invoice SET status = 'Paid', paid_amount = @invGrandB, remaining_amount = 0 WHERE id = @invIdB;

    UPDATE sales_order SET status = 'In Delivery' WHERE order_id = @orderIdB;

    DECLARE @doDateB DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 4), @receiptDateB);
    DECLARE @delCatIdB INT;
    SELECT TOP 1 @delCatIdB = id FROM delivery_category ORDER BY NEWID();

    INSERT INTO delivery_order_header (customer_id, do_number, delivery_category_id, po_number, address, notes, do_date, so_id, status)
    VALUES (
        @custIdB,
        'SJ-SIDANG-' + RIGHT('00000' + CAST(@b AS VARCHAR), 5),
        @delCatIdB,
        'PO-SD-' + CAST(1000 + @b AS VARCHAR),
        @custAddrB,
        'Sample delivery from fully-paid SO (sidang demo)',
        @doDateB,
        @orderIdB,
        'In Delivery'
    );
    DECLARE @doIdB INT = CAST(SCOPE_IDENTITY() AS INT);

    -- Bucket E (iter 66-85): berhenti di DO "In Delivery" -- qty kirim
    -- belum relevan (belum ada baris detail sampai DO diproses).
    IF @b <= 85
    BEGIN
        INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
        SELECT @doIdB, sod.product_id, sod.product_qty, sod.product_qty, sod.warehouse_id
        FROM sales_order_detail sod
        WHERE sod.order_id = @orderIdB;

        SET @b = @b + 1;
        CONTINUE;
    END

    -- Bucket F (iter 86-92): DO diterima PENUH -> SO Completed
    IF @b <= 92
    BEGIN
        INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
        SELECT @doIdB, sod.product_id, sod.product_qty, sod.product_qty, sod.warehouse_id
        FROM sales_order_detail sod
        WHERE sod.order_id = @orderIdB;

        UPDATE delivery_order_header SET status = 'Received' WHERE id = @doIdB;
        UPDATE sales_order SET status = 'Completed' WHERE order_id = @orderIdB;

        SET @b = @b + 1;
        CONTINUE;
    END

    -- Bucket G (iter 93-100): DO diterima SEBAGIAN (qty kirim < qty pesan
    -- untuk sebagian item) -> SO Partially Fulfilled
    INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
    SELECT
        @doIdB,
        sod.product_id,
        CASE WHEN sod.product_qty > 1 THEN sod.product_qty - 1 ELSE sod.product_qty END,
        sod.product_qty,
        sod.warehouse_id
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdB;

    UPDATE delivery_order_header SET status = 'Received' WHERE id = @doIdB;
    UPDATE sales_order SET status = 'Partially Fulfilled' WHERE order_id = @orderIdB;

    SET @b = @b + 1;
END

-- ------------------------------------------------------------
-- 4. Loop C -- 15 SO barang indent (is_indent = 1, TANPA quotation),
--    Uang Muka -> 2x Invoice Proforma (DP 30% / Pelunasan 70%) -> DO.
--    Sub-bucket (rentang iterasi @c):
--      1-3   : Invoice DP dibuat, Issued (belum dibayar).      SO: Processing
--      4-6   : Invoice DP Partially Paid.                      SO: Processing
--      7-8   : Invoice DP LUNAS -> Invoice Final dibuat, Issued (belum dibayar)
--      9-10  : Invoice DP LUNAS -> Invoice Final Partially Paid
--      11-12 : Kedua invoice LUNAS -> Delivery Order "In Delivery"
--      13-14 : Kedua invoice LUNAS -> DO diterima PENUH -> SO Completed
--      15    : Kedua invoice LUNAS -> DO diterima SEBAGIAN -> SO Partially Fulfilled
-- ------------------------------------------------------------
DECLARE @c INT = 1;
WHILE @c <= 15
BEGIN
    DECLARE @custIdC INT;
    SELECT TOP 1 @custIdC = customer_id FROM @demoCustomerIds ORDER BY NEWID();
    DECLARE @custAddrC NVARCHAR(500) = (SELECT alamat FROM master_customer WHERE customer_id = @custIdC);

    DECLARE @soDateC DATETIME = DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 240), GETDATE());
    DECLARE @kirimDateC DATETIME = DATEADD(DAY, 10 + (ABS(CHECKSUM(NEWID())) % 15), @soDateC);

    -- 4a. Sales Order barang indent -- status awal "Draft"
    INSERT INTO sales_order (so_number, tanggal_kirim, so_date, po_number, subtotal, customer_id, is_taxable, is_tax_included, address, notes, discount_total, tax_total, status, quotation_id, is_indent)
    VALUES (
        'SO-SIDANG-' + RIGHT('00000' + CAST(2000 + @c AS VARCHAR), 5),
        @kirimDateC, @soDateC,
        'PO-SD-' + CAST(2000 + @c AS VARCHAR),
        0,
        @custIdC, 1, 0,
        @custAddrC,
        'Sample indent order (made-to-order, sidang demo)',
        0, 0,
        'Draft',
        NULL,
        1
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

    -- 4b. Uang Muka dari SO ini -- nominal = 30% dari total SO.
    DECLARE @soNumberC VARCHAR(50) = (SELECT so_number FROM sales_order WHERE order_id = @orderIdC);
    DECLARE @dpDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @soDateC);
    DECLARE @dpAmountC DECIMAL(18,2) = ROUND(@soTotalC * 0.3, 0);
    DECLARE @dpFactorC DECIMAL(9,6) = CASE WHEN @soTotalC = 0 THEN 0.3 ELSE @dpAmountC / @soTotalC END;

    INSERT INTO uang_muka (NoFaktur, Tanggal, CustomerId, NoPO, NoSo, NominalUangMuka, IsTaxable, IsTaxIncluded, TaxAmount, TotalAmount, SyaratPembayaran, Alamat, Keterangan, Status, CreatedAt, UpdatedAt, CreatedBy)
    VALUES (
        'UM-SIDANG-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @dpDateC, @custIdC,
        'PO-SD-' + CAST(2000 + @c AS VARCHAR),
        @soNumberC,
        @dpAmountC, 0, 0, 0, @dpAmountC,
        'Net 30',
        @custAddrC,
        'Sample down payment (30%) for indent order (sidang demo)',
        'Draft',
        GETDATE(), GETDATE(), 'system-seed'
    );
    DECLARE @umIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    -- 4c. Invoice Proforma DP (proforma_stage = 'DP')
    UPDATE sales_order SET status = 'Processing' WHERE order_id = @orderIdC;

    DECLARE @invDpDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 3), @dpDateC);
    DECLARE @dueDpDateC DATETIME = DATEADD(DAY, 14, @invDpDateC);
    DECLARE @dpTaxC DECIMAL(18,2) = @dpAmountC * 0.11;
    DECLARE @dpGrandC DECIMAL(18,2) = @dpAmountC + @dpTaxC;

    INSERT INTO sales_invoice (invoice_number, customer_id, sales_order_id, delivery_order_id, invoice_date, due_date, status, subtotal, discount_total, tax_total, down_payment_amount, shipping_cost, grand_total, paid_amount, remaining_amount, notes, created_by, created_at, updated_at, proforma_stage)
    VALUES (
        'INV-SIDANG-DP-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @custIdC, @orderIdC, NULL,
        @invDpDateC, @dueDpDateC, 'Issued',
        @dpAmountC, 0, @dpTaxC, 0, 0, @dpGrandC, 0, @dpGrandC,
        'Invoice Proforma DP 30% (sidang demo)', 'system-seed', GETDATE(), GETDATE(),
        'DP'
    );
    DECLARE @invDpIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO sales_invoice_detail (sales_invoice_id, product_id, description, quantity, uom_id, price, discount, tax, subtotal, warehouse_id, created_at, updated_at)
    SELECT @invDpIdC, sod.product_id, sod.product_name, sod.product_qty, sod.uom_id,
           sod.product_price * @dpFactorC,
           0,
           sod.total_price * @dpFactorC * 0.11,
           sod.total_price * @dpFactorC,
           sod.warehouse_id, GETDATE(), GETDATE()
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdC;

    -- Sub-bucket (iter 1-3): berhenti di Invoice DP Issued, unpaid
    IF @c <= 3
    BEGIN
        SET @c = @c + 1;
        CONTINUE;
    END

    DECLARE @bankIdC INT;
    SELECT TOP 1 @bankIdC = id FROM bank ORDER BY NEWID();

    -- Sub-bucket (iter 4-6): Invoice DP Partially Paid
    IF @c <= 6
    BEGIN
        DECLARE @dpPaidPctC DECIMAL(5,4) = 0.3 + ((ABS(CHECKSUM(NEWID())) % 40) / 100.0);
        DECLARE @dpPaidAmtC DECIMAL(18,2) = ROUND(@dpGrandC * @dpPaidPctC, 0);
        DECLARE @dpReceiptDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @invDpDateC);

        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, uang_muka_id, sales_order_id, sales_invoice_id, status)
        VALUES (
            'BP-SIDANG-DP-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
            @custIdC, @bankIdC, @dpPaidAmtC, @dpReceiptDateC,
            @umIdC, @orderIdC, @invDpIdC, 'Validated'
        );

        UPDATE sales_invoice SET status = 'Partially Paid', paid_amount = @dpPaidAmtC, remaining_amount = @dpGrandC - @dpPaidAmtC WHERE id = @invDpIdC;

        SET @c = @c + 1;
        CONTINUE;
    END

    -- Iter 7+: Invoice DP LUNAS -> Uang Muka jadi Received
    DECLARE @dpReceiptDateC2 DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @invDpDateC);

    INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, uang_muka_id, sales_order_id, sales_invoice_id, status)
    VALUES (
        'BP-SIDANG-DP-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @custIdC, @bankIdC, @dpGrandC, @dpReceiptDateC2,
        @umIdC, @orderIdC, @invDpIdC, 'Validated'
    );

    UPDATE sales_invoice SET status = 'Paid', paid_amount = @dpGrandC, remaining_amount = 0 WHERE id = @invDpIdC;
    UPDATE uang_muka SET Status = 'Received', UpdatedAt = GETDATE() WHERE Id = @umIdC;

    -- 4d. Invoice Proforma Pelunasan (proforma_stage = 'Final')
    DECLARE @finalFactorC DECIMAL(9,6) = 1 - @dpFactorC;
    DECLARE @finalAmountC DECIMAL(18,2) = @soTotalC - @dpAmountC;
    DECLARE @invFinalDateC DATETIME = DATEADD(DAY, 3 + (ABS(CHECKSUM(NEWID())) % 10), @dpReceiptDateC2);
    DECLARE @dueFinalDateC DATETIME = DATEADD(DAY, 14, @invFinalDateC);
    DECLARE @finalTaxC DECIMAL(18,2) = @finalAmountC * 0.11;
    DECLARE @finalGrandC DECIMAL(18,2) = @finalAmountC + @finalTaxC;

    INSERT INTO sales_invoice (invoice_number, customer_id, sales_order_id, delivery_order_id, invoice_date, due_date, status, subtotal, discount_total, tax_total, down_payment_amount, shipping_cost, grand_total, paid_amount, remaining_amount, notes, created_by, created_at, updated_at, proforma_stage)
    VALUES (
        'INV-SIDANG-FINAL-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @custIdC, @orderIdC, NULL,
        @invFinalDateC, @dueFinalDateC, 'Issued',
        @finalAmountC, 0, @finalTaxC, 0, 0, @finalGrandC, 0, @finalGrandC,
        'Invoice Proforma Pelunasan 70% (sidang demo)', 'system-seed', GETDATE(), GETDATE(),
        'Final'
    );
    DECLARE @invFinalIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO sales_invoice_detail (sales_invoice_id, product_id, description, quantity, uom_id, price, discount, tax, subtotal, warehouse_id, created_at, updated_at)
    SELECT @invFinalIdC, sod.product_id, sod.product_name, sod.product_qty, sod.uom_id,
           sod.product_price * @finalFactorC,
           0,
           sod.total_price * @finalFactorC * 0.11,
           sod.total_price * @finalFactorC,
           sod.warehouse_id, GETDATE(), GETDATE()
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdC;

    -- Sub-bucket (iter 7-8): berhenti di Invoice Final Issued, unpaid
    IF @c <= 8
    BEGIN
        SET @c = @c + 1;
        CONTINUE;
    END

    DECLARE @bankIdC2 INT;
    SELECT TOP 1 @bankIdC2 = id FROM bank ORDER BY NEWID();

    -- Sub-bucket (iter 9-10): Invoice Final Partially Paid
    IF @c <= 10
    BEGIN
        DECLARE @finalPaidPctC DECIMAL(5,4) = 0.3 + ((ABS(CHECKSUM(NEWID())) % 40) / 100.0);
        DECLARE @finalPaidAmtC DECIMAL(18,2) = ROUND(@finalGrandC * @finalPaidPctC, 0);
        DECLARE @finalReceiptDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @invFinalDateC);

        INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
        VALUES (
            'BP-SIDANG-FINAL-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
            @custIdC, @bankIdC2, @finalPaidAmtC, @finalReceiptDateC,
            @orderIdC, @invFinalIdC, 'Validated'
        );

        UPDATE sales_invoice SET status = 'Partially Paid', paid_amount = @finalPaidAmtC, remaining_amount = @finalGrandC - @finalPaidAmtC WHERE id = @invFinalIdC;

        SET @c = @c + 1;
        CONTINUE;
    END

    -- Iter 11+: Invoice Final LUNAS -> kedua invoice lunas 100% -> DO boleh dibuat
    DECLARE @finalReceiptDateC2 DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 5), @invFinalDateC);

    INSERT INTO sales_receipt (no_bukti, customer_id, bank_id, nilai_pembayaran, tanggal_bayar, sales_order_id, sales_invoice_id, status)
    VALUES (
        'BP-SIDANG-FINAL-' + RIGHT('00000' + CAST(@c AS VARCHAR), 5),
        @custIdC, @bankIdC2, @finalGrandC, @finalReceiptDateC2,
        @orderIdC, @invFinalIdC, 'Validated'
    );

    UPDATE sales_invoice SET status = 'Paid', paid_amount = @finalGrandC, remaining_amount = 0 WHERE id = @invFinalIdC;

    UPDATE sales_order SET status = 'In Delivery' WHERE order_id = @orderIdC;

    DECLARE @doDateC DATETIME = DATEADD(DAY, 1 + (ABS(CHECKSUM(NEWID())) % 4), @finalReceiptDateC2);
    DECLARE @delCatIdC INT;
    SELECT TOP 1 @delCatIdC = id FROM delivery_category ORDER BY NEWID();

    INSERT INTO delivery_order_header (customer_id, do_number, delivery_category_id, po_number, address, notes, do_date, so_id, status)
    VALUES (
        @custIdC,
        'SJ-SIDANG-' + RIGHT('00000' + CAST(2000 + @c AS VARCHAR), 5),
        @delCatIdC,
        'PO-SD-' + CAST(2000 + @c AS VARCHAR),
        @custAddrC,
        'Sample delivery from fully-paid indent SO (sidang demo)',
        @doDateC,
        @orderIdC,
        'In Delivery'
    );
    DECLARE @doIdC INT = CAST(SCOPE_IDENTITY() AS INT);

    -- Sub-bucket (iter 11-12): berhenti di DO "In Delivery"
    IF @c <= 12
    BEGIN
        INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
        SELECT @doIdC, sod.product_id, sod.product_qty, sod.product_qty, sod.warehouse_id
        FROM sales_order_detail sod
        WHERE sod.order_id = @orderIdC;

        SET @c = @c + 1;
        CONTINUE;
    END

    -- Sub-bucket (iter 13-14): DO diterima PENUH -> SO Completed
    IF @c <= 14
    BEGIN
        INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
        SELECT @doIdC, sod.product_id, sod.product_qty, sod.product_qty, sod.warehouse_id
        FROM sales_order_detail sod
        WHERE sod.order_id = @orderIdC;

        UPDATE delivery_order_header SET status = 'Received' WHERE id = @doIdC;
        UPDATE sales_order SET status = 'Completed' WHERE order_id = @orderIdC;

        SET @c = @c + 1;
        CONTINUE;
    END

    -- Iter 15: DO diterima SEBAGIAN -> SO Partially Fulfilled
    INSERT INTO delivery_order_detail (delivery_id, product_id, qty_dikirim, qty_dipesan, warehouse_id)
    SELECT
        @doIdC,
        sod.product_id,
        CASE WHEN sod.product_qty > 1 THEN sod.product_qty - 1 ELSE sod.product_qty END,
        sod.product_qty,
        sod.warehouse_id
    FROM sales_order_detail sod
    WHERE sod.order_id = @orderIdC;

    UPDATE delivery_order_header SET status = 'Received' WHERE id = @doIdC;
    UPDATE sales_order SET status = 'Partially Fulfilled' WHERE order_id = @orderIdC;

    SET @c = @c + 1;
END

COMMIT TRANSACTION SeedSidangFull;

DECLARE @cntCust INT = (SELECT COUNT(*) FROM master_customer WHERE customer_code LIKE 'SIDANG-%');
DECLARE @cntQuot INT = (SELECT COUNT(*) FROM sales_quotation WHERE quotation_number LIKE 'SQ-SIDANG-%');
DECLARE @cntSO INT = (SELECT COUNT(*) FROM sales_order WHERE so_number LIKE 'SO-SIDANG-%');
DECLARE @cntSOIndent INT = (SELECT COUNT(*) FROM sales_order WHERE so_number LIKE 'SO-SIDANG-%' AND is_indent = 1);
DECLARE @cntSOPartial INT = (SELECT COUNT(*) FROM sales_order WHERE so_number LIKE 'SO-SIDANG-%' AND status = 'Partially Fulfilled');
DECLARE @cntDO INT = (SELECT COUNT(*) FROM delivery_order_header WHERE do_number LIKE 'SJ-SIDANG-%');
DECLARE @cntInv INT = (SELECT COUNT(*) FROM sales_invoice WHERE invoice_number LIKE 'INV-SIDANG-%');
DECLARE @cntReceipt INT = (SELECT COUNT(*) FROM sales_receipt WHERE no_bukti LIKE 'BP-SIDANG-%');
DECLARE @cntUM INT = (SELECT COUNT(*) FROM uang_muka WHERE NoFaktur LIKE 'UM-SIDANG-%');

PRINT 'Seed selesai.';
PRINT 'Customer      : ' + CAST(@cntCust AS VARCHAR);
PRINT 'Quotation     : ' + CAST(@cntQuot AS VARCHAR) + ' (20 berdiri sendiri + 100 jadi SO)';
PRINT 'Sales Order   : ' + CAST(@cntSO AS VARCHAR) + ' (termasuk ' + CAST(@cntSOIndent AS VARCHAR) + ' indent, ' + CAST(@cntSOPartial AS VARCHAR) + ' Partially Fulfilled)';
PRINT 'Delivery Order: ' + CAST(@cntDO AS VARCHAR);
PRINT 'Invoice       : ' + CAST(@cntInv AS VARCHAR);
PRINT 'Sales Receipt : ' + CAST(@cntReceipt AS VARCHAR);
PRINT 'Uang Muka     : ' + CAST(@cntUM AS VARCHAR);

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION SeedSidangFull;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
