-- ============================================================
-- Migration: Normalize inconsistent INV numbers to INV-XXXXXXXXXX
--
-- Handles all legacy formats, e.g.: INV000001, INV000002, etc.
--
-- Strategy:
--   Rows that already match INV-NNNNNNNNNN are left untouched.
--   Legacy rows are renumbered starting from
--   (MAX existing canonical number + 1), in ascending
--   purchase_invoice_id order, to avoid any collisions.
--
-- Run in DBeaver: Ctrl+A to select all, Ctrl+Enter to execute.
-- ============================================================

-- Step 1: Build the mapping old -> new
SELECT
    pi.purchase_invoice_id,
    pi.invoice_number AS old_invoice_number,
    'INV-' + RIGHT('0000000000' + CAST(
        (
            SELECT ISNULL(MAX(CAST(SUBSTRING(invoice_number, 5, 10) AS INT)), 0)
            FROM purchase_invoice
            WHERE invoice_number LIKE 'INV-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ) + rn.rn
    AS VARCHAR(10)), 10) AS new_invoice_number
INTO #inv_remap
FROM purchase_invoice pi
INNER JOIN (
    SELECT
        purchase_invoice_id,
        ROW_NUMBER() OVER (ORDER BY purchase_invoice_id ASC) AS rn
    FROM purchase_invoice
    WHERE invoice_number NOT LIKE 'INV-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON pi.purchase_invoice_id = rn.purchase_invoice_id

-- Step 2: Update purchase_invoice
UPDATE pi
SET pi.invoice_number = r.new_invoice_number
FROM purchase_invoice pi
INNER JOIN #inv_remap r ON pi.purchase_invoice_id = r.purchase_invoice_id

-- Step 3: Cleanup
DROP TABLE #inv_remap

-- Step 4: Verify - should return 0 rows
SELECT purchase_invoice_id, invoice_number
FROM purchase_invoice
WHERE invoice_number NOT LIKE 'INV-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
