-- Migration: Rename purchase_return_number prefix from PR- to RTN-
-- Run this script once against your database.
-- Safe to re-run: the WHERE clause only targets rows that still have the PR- prefix.

UPDATE purchase_return
SET purchase_return_number = 'RTN-' + SUBSTRING(purchase_return_number, 4, LEN(purchase_return_number))
WHERE purchase_return_number LIKE 'PR-%';
