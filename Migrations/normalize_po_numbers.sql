-- ============================================================
-- Migration: Normalize inconsistent PO numbers to PO-XXXXXXXXXX
-- Run all statements together by selecting all (Ctrl+A) then
-- executing (Ctrl+Enter) in DBeaver.
-- ============================================================

-- Step 1: Build the mapping old -> new
SELECT
    po.purchase_order_id,
    po.po_number AS old_po_number,
    'PO-' + RIGHT('0000000000' + CAST(
        (
            SELECT ISNULL(MAX(CAST(SUBSTRING(po_number, 4, 10) AS INT)), 0)
            FROM purchase_order
            WHERE po_number LIKE 'PO-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ) + rn.rn
    AS VARCHAR(10)), 10) AS new_po_number
INTO #po_remap
FROM purchase_order po
INNER JOIN (
    SELECT
        purchase_order_id,
        ROW_NUMBER() OVER (ORDER BY purchase_order_id ASC) AS rn
    FROM purchase_order
    WHERE po_number NOT LIKE 'PO-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON po.purchase_order_id = rn.purchase_order_id

-- Step 2: Update purchase_order
UPDATE po
SET po.po_number = r.new_po_number
FROM purchase_order po
INNER JOIN #po_remap r ON po.purchase_order_id = r.purchase_order_id

-- Step 3: Sync purchase_return.purchase_order_number
UPDATE pr
SET pr.purchase_order_number = r.new_po_number
FROM purchase_return pr
INNER JOIN #po_remap r ON pr.purchase_order_number = r.old_po_number

-- Step 4: Cleanup
DROP TABLE #po_remap

-- Step 5: Verify - should return 0 rows
SELECT purchase_order_id, po_number
FROM purchase_order
WHERE po_number NOT LIKE 'PO-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
