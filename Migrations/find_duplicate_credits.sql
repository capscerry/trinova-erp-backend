-- Find every Cash Refund Credit that is a duplicate of a same-day, same-amount Return Credit
-- on the same invoice. These are the spurious backend-inserted rows to delete.
SELECT
    crc.purchase_payment_id  AS duplicate_id,
    crc.payment_number       AS duplicate_number,
    rc.purchase_payment_id   AS keeper_id,
    rc.payment_number        AS keeper_number,
    rc.purchase_invoice_id,
    rc.amount,
    CAST(rc.payment_date AS DATE) AS payment_date
FROM purchase_payment rc
JOIN purchase_payment crc
    ON  rc.purchase_invoice_id = crc.purchase_invoice_id
    AND rc.amount              = crc.amount
    AND CAST(rc.payment_date  AS DATE) = CAST(crc.payment_date AS DATE)
WHERE rc.payment_method  = 'Return Credit'
  AND crc.payment_method = 'Cash Refund Credit'
ORDER BY rc.purchase_invoice_id;
