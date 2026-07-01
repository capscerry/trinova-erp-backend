-- ============================================================
-- Migration: Normalize inconsistent GR numbers to GR-XXXXXXXXXX
--
-- Handles all legacy formats, e.g.: GR000001, GR000002, etc.
--
-- Strategy:
--   Rows that already match GR-NNNNNNNNNN are left untouched.
--   Legacy rows are renumbered starting from
--   (MAX existing canonical number + 1), in ascending
--   goods_receipt_id order, to avoid any collisions.
--
-- Run in DBeaver: Ctrl+A to select all, Ctrl+Enter to execute.
-- ============================================================

-- Step 1: Build the mapping old -> new
SELECT
    gr.goods_receipt_id,
    gr.receipt_number AS old_receipt_number,
    'GR-' + RIGHT('0000000000' + CAST(
        (
            SELECT ISNULL(MAX(CAST(SUBSTRING(receipt_number, 4, 10) AS INT)), 0)
            FROM goods_receipt
            WHERE receipt_number LIKE 'GR-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ) + rn.rn
    AS VARCHAR(10)), 10) AS new_receipt_number
INTO #gr_remap
FROM goods_receipt gr
INNER JOIN (
    SELECT
        goods_receipt_id,
        ROW_NUMBER() OVER (ORDER BY goods_receipt_id ASC) AS rn
    FROM goods_receipt
    WHERE receipt_number NOT LIKE 'GR-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON gr.goods_receipt_id = rn.goods_receipt_id

-- Step 2: Update goods_receipt
UPDATE gr
SET gr.receipt_number = r.new_receipt_number
FROM goods_receipt gr
INNER JOIN #gr_remap r ON gr.goods_receipt_id = r.goods_receipt_id

-- Step 3: Cleanup
DROP TABLE #gr_remap

-- Step 4: Verify - should return 0 rows
SELECT goods_receipt_id, receipt_number
FROM goods_receipt
WHERE receipt_number NOT LIKE 'GR-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
