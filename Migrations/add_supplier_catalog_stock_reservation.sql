-- =============================================================================
-- Migration: Supplier Catalog Stock & Reservation Separation
-- =============================================================================
-- Purpose:
--   Separates the supplier catalog snapshot from PO reservation tracking.
--
--   Before this migration, available_stock served two roles:
--     (a) the latest stock reported by the supplier (catalog snapshot)
--     (b) residual = catalog - reserved  (reservation tracking)
--
--   This caused two bugs:
--     1. Re-uploading a catalog would overwrite available_stock and silently
--        wipe out any outstanding PO reservations.
--     2. The GoodsReceiptDetail sync was overwriting supplier_products.available_stock
--        with internal warehouse inventory totals — a completely different concept.
--
--   After this migration:
--     catalog_stock  = immutable snapshot set only by Excel catalog upload
--     reserved_qty   = sum of outstanding PO quantity not yet received
--     available_to_order (computed at query time) = catalog_stock - reserved_qty
--     available_stock is preserved for backward compatibility but no longer used
--                     by the reservation logic.
-- =============================================================================

-- Step 1: Add catalog_stock column
-- Defaults to the current available_stock value so existing rows are not lost.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('supplier_products')
      AND name = 'catalog_stock'
)
BEGIN
    ALTER TABLE supplier_products
    ADD catalog_stock INT NOT NULL DEFAULT 0;

    -- Back-fill: treat current available_stock as the initial catalog snapshot.
    -- This is an approximation — if a PO was already approved before this
    -- migration, available_stock will already be decremented.  The back-fill
    -- is intentionally conservative; operators can re-upload their catalogs
    -- to correct the values.
    UPDATE supplier_products
    SET catalog_stock = available_stock;

    PRINT 'catalog_stock column added and back-filled.';
END
ELSE
BEGIN
    PRINT 'catalog_stock column already exists — skipped.';
END;

-- Step 2: Add reserved_qty column
-- Starts at 0 for all existing rows.  Outstanding reservations from already-
-- approved POs will be reconciled when ApprovePurchaseOrder next runs, or
-- operators can re-approve existing POs to rebuild the reservation totals.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('supplier_products')
      AND name = 'reserved_qty'
)
BEGIN
    ALTER TABLE supplier_products
    ADD reserved_qty INT NOT NULL DEFAULT 0;

    PRINT 'reserved_qty column added.';
END
ELSE
BEGIN
    PRINT 'reserved_qty column already exists — skipped.';
END;

-- Step 3: Add updated_at column for catalog upload audit trail
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('supplier_products')
      AND name = 'updated_at'
)
BEGIN
    ALTER TABLE supplier_products
    ADD updated_at DATETIME NULL;

    PRINT 'updated_at column added.';
END
ELSE
BEGIN
    PRINT 'updated_at column already exists — skipped.';
END;

-- Step 4: Optional — add a check constraint so reserved_qty never goes negative
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('supplier_products')
      AND name = 'CK_supplier_products_reserved_qty_non_negative'
)
BEGIN
    ALTER TABLE supplier_products
    ADD CONSTRAINT CK_supplier_products_reserved_qty_non_negative
        CHECK (reserved_qty >= 0);

    PRINT 'Check constraint on reserved_qty added.';
END
ELSE
BEGIN
    PRINT 'Check constraint already exists — skipped.';
END;

-- =============================================================================
-- Verification
-- =============================================================================
SELECT
    supplier_product_id,
    supplier_id,
    product_id,
    available_stock,
    catalog_stock,
    reserved_qty,
    CASE
        WHEN catalog_stock - reserved_qty < 0 THEN 0
        ELSE catalog_stock - reserved_qty
    END AS available_to_order,
    updated_at
FROM supplier_products
ORDER BY supplier_id, product_id;
