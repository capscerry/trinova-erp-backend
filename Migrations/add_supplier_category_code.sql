-- ============================================================
-- Migration: Add category_code (SUC-XXXXXXXXXX) to supplier_category
-- Run all statements together by selecting all (Ctrl+A) then
-- executing (Ctrl+Enter) in DBeaver.
-- ============================================================

-- Step 1: Add the column (nullable first so existing rows don't fail)
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME  = 'supplier_category'
      AND COLUMN_NAME = 'category_code'
)
BEGIN
    ALTER TABLE supplier_category
    ADD category_code NVARCHAR(20) NULL;
END

-- Step 2: Back-fill existing rows in category_id order
--         Each row gets SUC-0000000001, SUC-0000000002, ...
UPDATE sc
SET sc.category_code = 'SUC-' + RIGHT(
    '0000000000' + CAST(rn.rn AS NVARCHAR(10)),
    10
)
FROM supplier_category sc
INNER JOIN (
    SELECT
        category_id,
        ROW_NUMBER() OVER (ORDER BY category_id ASC) AS rn
    FROM supplier_category
    WHERE category_code IS NULL
       OR category_code NOT LIKE 'SUC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON sc.category_id = rn.category_id;

-- Step 3: Make the column NOT NULL and UNIQUE now that every row has a value
ALTER TABLE supplier_category
    ALTER COLUMN category_code NVARCHAR(20) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name        = 'UQ_supplier_category_code'
      AND object_id   = OBJECT_ID('supplier_category')
)
BEGIN
    ALTER TABLE supplier_category
    ADD CONSTRAINT UQ_supplier_category_code
    UNIQUE (category_code);
END

-- Step 4: Verify — should return 0 rows
SELECT category_id, category_name, category_code
FROM supplier_category
WHERE category_code NOT LIKE 'SUC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';
