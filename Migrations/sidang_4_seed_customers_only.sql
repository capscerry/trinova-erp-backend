-- ============================================================
-- LANGKAH 4/4a — Data dummy KHUSUS customer + customer category.
--
-- Jalankan ini kalau cuma butuh master data customer terisi (misal
-- mau input transaksi manual sendiri lewat UI untuk demo sidang),
-- TANPA transaksi (quotation/SO/invoice/dst) ikut dibuat otomatis.
-- Kalau mau semuanya sekalian, jalankan sidang_5_seed_full_dummy_data.sql
-- (yang juga butuh customer -- akan membuat sendiri kalau belum ada).
--
-- Semua baris ditandai prefix 'SIDANG-' supaya gampang dibedakan dan
-- dihapus lagi lewat sidang_cleanup_dummy_data.sql.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION SeedSidangCustomers;

BEGIN TRY

    -- ------------------------------------------------------------
    -- 1. Customer Category
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

    -- ------------------------------------------------------------
    -- 2. 25 Customer, tersebar rata ke tiap kategori di atas
    -- ------------------------------------------------------------
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

    COMMIT TRANSACTION SeedSidangCustomers;

    DECLARE @cnt INT = (SELECT COUNT(*) FROM master_customer WHERE customer_code LIKE 'SIDANG-%');
    PRINT 'Seed customer selesai. Total customer SIDANG-: ' + CAST(@cnt AS VARCHAR);

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION SeedSidangCustomers;
    PRINT 'Terjadi error, transaksi di-rollback:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
