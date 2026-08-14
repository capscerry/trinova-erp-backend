-- ============================================================
-- Migration: create_purchase_return_item
-- Purpose  : Persist the individual line items (product_id,
--            qty_return, unit_price, subtotal) for each
--            Purchase Return so that the settlement modal and
--            stock-restore logic always use the exact quantities
--            the user set — not the full GR quantities.
-- ============================================================

CREATE TABLE purchase_return_item (
    purchase_return_item_id INT           IDENTITY(1,1) NOT NULL,
    purchase_return_id      INT           NOT NULL,
    product_id              INT           NOT NULL,
    product_name            NVARCHAR(255) NOT NULL DEFAULT '',
    qty_return              INT           NOT NULL,
    unit_price              DECIMAL(18,2) NOT NULL DEFAULT 0,
    subtotal                DECIMAL(18,2) NOT NULL DEFAULT 0,

    CONSTRAINT PK_purchase_return_item
        PRIMARY KEY (purchase_return_item_id),

    CONSTRAINT FK_purchase_return_item_return
        FOREIGN KEY (purchase_return_id)
        REFERENCES purchase_return (purchase_return_id)
        ON DELETE CASCADE
);

CREATE INDEX IX_purchase_return_item_return_id
    ON purchase_return_item (purchase_return_id);

-- ============================================================
-- Back-fill: seed rows for existing returns whose items are
-- stored as JSON in transaction_detail but have no child rows.
-- Only inserts where the column is valid JSON and contains at
-- least one element with qty_return > 0.
-- ============================================================
INSERT INTO purchase_return_item
(
    purchase_return_id,
    product_id,
    product_name,
    qty_return,
    unit_price,
    subtotal
)
SELECT
    pr.purchase_return_id,
    CAST(item.value AS INT)                                    AS product_id,
    ISNULL(JSON_VALUE(item.value, '$.product_name'), '')       AS product_name,
    CAST(JSON_VALUE(item.value, '$.qty_return')   AS INT)      AS qty_return,
    ISNULL(CAST(JSON_VALUE(item.value, '$.unit_price') AS DECIMAL(18,2)), 0) AS unit_price,
    ISNULL(CAST(JSON_VALUE(item.value, '$.subtotal')   AS DECIMAL(18,2)), 0) AS subtotal
FROM purchase_return pr
CROSS APPLY OPENJSON(pr.transaction_detail) WITH (
    product_id   INT            '$.product_id',
    product_name NVARCHAR(255)  '$.product_name',
    qty_return   INT            '$.qty_return',
    unit_price   DECIMAL(18,2)  '$.unit_price',
    subtotal     DECIMAL(18,2)  '$.subtotal'
) AS item
WHERE
    -- skip returns that already have child rows (idempotent re-run)
    NOT EXISTS (
        SELECT 1
        FROM purchase_return_item pri
        WHERE pri.purchase_return_id = pr.purchase_return_id
    )
    -- only process rows with valid, non-empty JSON arrays
    AND ISJSON(pr.transaction_detail) = 1
    AND JSON_VALUE(pr.transaction_detail, '$[0].qty_return') IS NOT NULL
    AND CAST(JSON_VALUE(pr.transaction_detail, '$[0].qty_return') AS INT) > 0;
