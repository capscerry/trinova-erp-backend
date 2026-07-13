-- Migration: add product_type column to master_product
-- The application model and repository both reference product_type
-- but the column was never added to the database table.

IF COL_LENGTH('master_product', 'product_type') IS NULL
BEGIN
    ALTER TABLE master_product
    ADD product_type VARCHAR(100) NULL;

    PRINT 'Column product_type added to master_product.';
END
ELSE
BEGIN
    PRINT 'Column product_type already exists, skipping.';
END
