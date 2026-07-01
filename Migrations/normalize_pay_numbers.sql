-- ============================================================
-- Migration: Normalize inconsistent PAY numbers to PAY-XXXXXXXXXX
--
-- Handles all legacy formats, e.g.: PAY000001, PAY000002, etc.
--
-- Strategy:
--   Rows that already match PAY-NNNNNNNNNN are left untouched.
--   Legacy rows are renumbered starting from
--   (MAX existing canonical number + 1), in ascending
--   purchase_payment_id order, to avoid any collisions.
--
-- Run in DBeaver: Ctrl+A to select all, Ctrl+Enter to execute.
-- ============================================================

-- Step 1: Build the mapping old -> new
SELECT
    pp.purchase_payment_id,
    pp.payment_number AS old_payment_number,
    'PAY-' + RIGHT('0000000000' + CAST(
        (
            SELECT ISNULL(MAX(CAST(SUBSTRING(payment_number, 5, 10) AS INT)), 0)
            FROM purchase_payment
            WHERE payment_number LIKE 'PAY-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ) + rn.rn
    AS VARCHAR(10)), 10) AS new_payment_number
INTO #pay_remap
FROM purchase_payment pp
INNER JOIN (
    SELECT
        purchase_payment_id,
        ROW_NUMBER() OVER (ORDER BY purchase_payment_id ASC) AS rn
    FROM purchase_payment
    WHERE payment_number NOT LIKE 'PAY-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON pp.purchase_payment_id = rn.purchase_payment_id

-- Step 2: Update purchase_payment
UPDATE pp
SET pp.payment_number = r.new_payment_number
FROM purchase_payment pp
INNER JOIN #pay_remap r ON pp.purchase_payment_id = r.purchase_payment_id

-- Step 3: Cleanup
DROP TABLE #pay_remap

-- Step 4: Verify - should return 0 rows
SELECT purchase_payment_id, payment_number
FROM purchase_payment
WHERE payment_number NOT LIKE 'PAY-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
