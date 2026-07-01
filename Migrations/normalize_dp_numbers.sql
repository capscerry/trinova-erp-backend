-- ============================================================
-- Migration: Normalize inconsistent DP numbers to DP-XXXXXXXXXX
--
-- Handles all legacy formats, e.g.: DP000001, DP000002, etc.
--
-- Strategy:
--   Rows that already match DP-NNNNNNNNNN are left untouched.
--   Legacy rows are renumbered starting from
--   (MAX existing canonical number + 1), in ascending
--   purchase_down_payment_id order, to avoid any collisions.
--
-- Run in DBeaver: Ctrl+A to select all, Ctrl+Enter to execute.
-- ============================================================

-- Step 1: Build the mapping old -> new
SELECT
    pdp.purchase_down_payment_id,
    pdp.dp_number AS old_dp_number,
    'DP-' + RIGHT('0000000000' + CAST(
        (
            SELECT ISNULL(MAX(CAST(SUBSTRING(dp_number, 4, 10) AS INT)), 0)
            FROM purchase_down_payment
            WHERE dp_number LIKE 'DP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ) + rn.rn
    AS VARCHAR(10)), 10) AS new_dp_number
INTO #dp_remap
FROM purchase_down_payment pdp
INNER JOIN (
    SELECT
        purchase_down_payment_id,
        ROW_NUMBER() OVER (ORDER BY purchase_down_payment_id ASC) AS rn
    FROM purchase_down_payment
    WHERE dp_number NOT LIKE 'DP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) rn ON pdp.purchase_down_payment_id = rn.purchase_down_payment_id

-- Step 2: Update purchase_down_payment
UPDATE pdp
SET pdp.dp_number = r.new_dp_number
FROM purchase_down_payment pdp
INNER JOIN #dp_remap r ON pdp.purchase_down_payment_id = r.purchase_down_payment_id

-- Step 3: Cleanup
DROP TABLE #dp_remap

-- Step 4: Verify - should return 0 rows
SELECT purchase_down_payment_id, dp_number
FROM purchase_down_payment
WHERE dp_number NOT LIKE 'DP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
