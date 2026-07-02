
$dll = "C:\Users\Davina\Desktop\trinova-erp-backend\bin\Debug\net8.0\Microsoft.Data.SqlClient.dll"
Add-Type -Path $dll

$cs = "Server=tcp:trinova-server-dev.database.windows.net,1433;Initial Catalog=trinova-dev;" +
      "User ID=capscerry@trinova-server-dev;Password=capstoneckrtgr11;" +
      "Encrypt=True;TrustServerCertificate=True;"

$conn = New-Object Microsoft.Data.SqlClient.SqlConnection $cs
$conn.Open()

# ── Step 1: find duplicates ──────────────────────────────────────────────────
$findSql = @"
SELECT
    crc.purchase_payment_id  AS duplicate_id,
    crc.payment_number       AS duplicate_number,
    rc.purchase_payment_id   AS keeper_id,
    rc.payment_number        AS keeper_number,
    rc.purchase_invoice_id   AS invoice_id,
    rc.amount                AS amount,
    CAST(rc.payment_date AS DATE) AS payment_date
FROM purchase_payment rc
JOIN purchase_payment crc
    ON  rc.purchase_invoice_id = crc.purchase_invoice_id
    AND rc.amount              = crc.amount
    AND CAST(rc.payment_date AS DATE) = CAST(crc.payment_date AS DATE)
WHERE rc.payment_method  = 'Return Credit'
  AND crc.payment_method = 'Cash Refund Credit'
ORDER BY rc.purchase_invoice_id
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $findSql
$reader = $cmd.ExecuteReader()

$rows = [System.Collections.Generic.List[PSCustomObject]]::new()
while ($reader.Read()) {
    $rows.Add([PSCustomObject]@{
        duplicate_id     = [int]$reader["duplicate_id"]
        duplicate_number = $reader["duplicate_number"].ToString()
        keeper_id        = [int]$reader["keeper_id"]
        keeper_number    = $reader["keeper_number"].ToString()
        invoice_id       = [int]$reader["invoice_id"]
        amount           = [decimal]$reader["amount"]
        payment_date     = $reader["payment_date"].ToString()
    })
}
$reader.Close()

Write-Host "=== Duplicate Cash Refund Credit rows found: $($rows.Count) ==="
$rows | Format-Table -AutoSize

if ($rows.Count -eq 0) {
    Write-Host "Nothing to delete."
    $conn.Close()
    exit 0
}

# ── Step 2: delete the spurious Cash Refund Credit rows ─────────────────────
$idsToDelete = ($rows | ForEach-Object { $_.duplicate_id }) -join ","
$deleteSql = "DELETE FROM purchase_payment WHERE purchase_payment_id IN ($idsToDelete)"

Write-Host ""
Write-Host "Deleting duplicate IDs: $idsToDelete"

$delCmd = $conn.CreateCommand()
$delCmd.CommandText = $deleteSql
$deleted = $delCmd.ExecuteNonQuery()

$conn.Close()
Write-Host "Deleted $deleted row(s). Done."
