using Microsoft.Data.SqlClient;

const string connectionString =
    "Server=trinova-server-dev.database.windows.net;" +
    "Database=trinova-dev;" +
    "User Id=capscerry;" +
    "Password=capstoneckrtgr@11;" +
    "TrustServerCertificate=True;";

// Each statement is a separate batch — run them in order.
// Step 1 is guarded: skip if the column already exists.
var steps = new[]
{
    // ── Step 1: Add column (nullable) ───────────────────────────────────────
    @"IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('goods_receipt_detail')
          AND name = 'remaining_qty'
    )
    BEGIN
        ALTER TABLE goods_receipt_detail
        ADD remaining_qty INT NULL;
        PRINT 'Step 1: column added.';
    END
    ELSE
    BEGIN
        PRINT 'Step 1: column already exists, skipped.';
    END",

    // ── Step 2a: Back-fill from settled JSON returns ────────────────────────
    // ISJSON guard prevents OPENJSON from throwing on plain-text legacy rows.
    @"UPDATE grd
    SET grd.remaining_qty = CASE
        WHEN grd.quantity - ISNULL(settled.total_returned, 0) < 0
            THEN 0
        ELSE grd.quantity - ISNULL(settled.total_returned, 0)
    END
    FROM goods_receipt_detail grd
    LEFT JOIN (
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
       AND settled.product_id       = grd.product_id
    WHERE grd.remaining_qty IS NULL;",

    // ── Step 2b: Legacy closed returns with no JSON → set to 0 ─────────────
    @"UPDATE grd
    SET grd.remaining_qty = 0
    FROM goods_receipt_detail grd
    INNER JOIN purchase_return pr
        ON pr.goods_receipt_id = grd.goods_receipt_id
    WHERE pr.status = 'Closed'
      AND (pr.transaction_detail IS NULL OR pr.transaction_detail = '' OR pr.transaction_detail = '[]')
      AND grd.remaining_qty IS NULL;",

    // ── Step 2c: Any remaining NULL rows → full qty available ───────────────
    @"UPDATE goods_receipt_detail
    SET remaining_qty = quantity
    WHERE remaining_qty IS NULL;",

    // ── Step 3: Make NOT NULL now every row has a value ─────────────────────
    @"ALTER TABLE goods_receipt_detail
    ALTER COLUMN remaining_qty INT NOT NULL;",
};

Console.WriteLine("Connecting to database…");
using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();
Console.WriteLine("Connected.\n");

for (int i = 0; i < steps.Length; i++)
{
    Console.Write($"  Step {i + 1}/{steps.Length} … ");
    try
    {
        using var cmd = new SqlCommand(steps[i], conn);
        cmd.CommandTimeout = 120;
        int rows = await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"OK ({rows} row(s) affected)");
    }
    catch (SqlException ex)
    {
        // Already NOT NULL is harmless — the column was promoted in a prior run
        if (ex.Message.Contains("already") || ex.Message.Contains("There is already"))
        {
            Console.WriteLine($"SKIPPED — {ex.Message.Split('\n')[0]}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED\n  {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}

Console.WriteLine("\nMigration complete.\n");

// ── Quick sanity check ──────────────────────────────────────────────────────
Console.WriteLine("Verification:");

// 1. Confirm column exists
using (var cmd = new SqlCommand(
    "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('goods_receipt_detail') AND name = 'remaining_qty'",
    conn))
{
    int exists = (int)(await cmd.ExecuteScalarAsync())!;
    Console.WriteLine($"  Column remaining_qty exists: {(exists > 0 ? "YES ✓" : "NO ✗")}");
}

// 2. Check for any NULLs (should be zero)
using (var cmd = new SqlCommand(
    "SELECT COUNT(*) FROM goods_receipt_detail WHERE remaining_qty IS NULL",
    conn))
{
    int nullCount = (int)(await cmd.ExecuteScalarAsync())!;
    Console.WriteLine($"  Rows with NULL remaining_qty: {nullCount} {(nullCount == 0 ? "✓" : "✗ — unexpected!")}");
}

// 3. Print 5 sample rows
Console.WriteLine("\n  Sample rows (5 most recent):");
Console.WriteLine($"  {"detailId",-10} {"gr_id",-8} {"product_id",-12} {"qty",-8} {"remaining_qty",-14}");
using (var cmd = new SqlCommand(
    "SELECT TOP 5 goods_receipt_detail_id, goods_receipt_id, product_id, quantity, remaining_qty FROM goods_receipt_detail ORDER BY goods_receipt_detail_id DESC",
    conn))
using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
        Console.WriteLine($"  {r[0],-10} {r[1],-8} {r[2],-12} {r[3],-8} {r[4],-14}");
}

return 0;
