-- Migration: Add remaining_qty column to goods_receipt_detail
-- This column tracks how many units are still available for Purchase Return.
-- It starts equal to quantity (the original received qty) and is reduced
-- each time a Purchase Return settlement is completed for that line.

-- Step 1: Add the column (nullable first so existing rows don't fail)
ALTER TABLE goods_receipt_detail
ADD remaining_qty INT NULL;

-- Step 2: Back-fill existing rows.
--   Subtract any already-settled return quantities so the column starts accurate.
--   A "settled" return is one whose status = 'Closed'.
--
--   If no settled returns exist for a GR line, remaining_qty = original quantity.
--   If settled returns partially consumed it, remaining_qty = quantity - returned.
--   Clamp at 0 to handle any over-return data that may already exist in prod.
UPDATE grd
SET grd.remaining_qty = CASE
    WHEN grd.quantity - ISNULL(settled.total_returned, 0) < 0
        THEN 0
    ELSE grd.quantity - ISNULL(settled.total_returned, 0)
END
FROM goods_receipt_detail grd
LEFT JOIN (
    -- Sum all qty_return values from settled purchase returns per product per GR.
    -- transaction_detail is a JSON array: [{"product_id":X,"qty_return":Y}, ...]
    -- We parse it here using a cross-apply + JSON path to get per-line totals.
    SELECT
        pr.goods_receipt_id,
        items.product_id,
        SUM(items.qty_return) AS total_returned
    FROM purchase_return pr
    CROSS APPLY (
        SELECT
            TRY_CAST(JSON_VALUE(j.value, '$.product_id') AS INT) AS product_id,
            TRY_CAST(JSON_VALUE(j.value, '$.qty_return')  AS INT) AS qty_return
        FROM OPENJSON(pr.transaction_detail) j
    ) items
    WHERE pr.status = 'Closed'
      AND ISJSON(pr.transaction_detail) = 1
      AND items.product_id IS NOT NULL
      AND items.qty_return  IS NOT NULL
      AND items.qty_return  > 0
    GROUP BY pr.goods_receipt_id, items.product_id
) settled
    ON settled.goods_receipt_id = grd.goods_receipt_id
   AND settled.product_id       = grd.product_id;

-- For rows where transaction_detail was empty / legacy (no JSON) back-fill
-- conservatively: if a Closed return exists for the whole GR, set remaining_qty = 0.
UPDATE grd
SET grd.remaining_qty = 0
FROM goods_receipt_detail grd
INNER JOIN purchase_return pr
    ON pr.goods_receipt_id = grd.goods_receipt_id
WHERE pr.status = 'Closed'
  AND (pr.transaction_detail IS NULL OR pr.transaction_detail = '' OR pr.transaction_detail = '[]')
  AND grd.remaining_qty IS NULL;   -- only touch rows not already set above

-- Fallback: any row still NULL means no returns ever touched it → full qty available
UPDATE goods_receipt_detail
SET remaining_qty = quantity
WHERE remaining_qty IS NULL;

-- Step 3: Make the column NOT NULL now that every row has a value
ALTER TABLE goods_receipt_detail
ALTER COLUMN remaining_qty INT NOT NULL;
