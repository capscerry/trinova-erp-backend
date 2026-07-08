-- Backfill: copy nomor_faktur_pajak from purchase_order into purchase_invoice
-- for all invoice rows that don't have one yet (NULL or empty string).
-- Safe to run multiple times.

UPDATE pi
SET pi.nomor_faktur_pajak = po.nomor_faktur_pajak
FROM purchase_invoice pi
LEFT JOIN goods_receipt gr
    ON pi.goods_receipt_id = gr.goods_receipt_id
LEFT JOIN purchase_order po
    ON gr.purchase_order_id = po.purchase_order_id
WHERE (pi.nomor_faktur_pajak IS NULL OR pi.nomor_faktur_pajak = '')
  AND po.nomor_faktur_pajak IS NOT NULL
  AND po.nomor_faktur_pajak <> '';
