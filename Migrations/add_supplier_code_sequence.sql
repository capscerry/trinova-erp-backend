-- ============================================================
-- Migration: add_supplier_code_sequence
-- Purpose  : Replace MAX()-based supplier code generation with
--            a concurrency-safe counter table.
--
-- Strategy : A single-row table (supplier_code_counter) acts as
--            a serialised counter. Every code generation grabs an
--            UPDLOCK + HOLDLOCK on that row inside a SERIALIZABLE
--            transaction, increments it, and returns the new value.
--            Concurrent sessions queue up behind the lock — no two
--            sessions can ever read the same counter value.
--
-- Run once : The script is idempotent — safe to execute multiple
--            times on the same database.
-- ============================================================

-- ── 1. Create the counter table (if it does not already exist) ──

IF OBJECT_ID(N'dbo.supplier_code_counter', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.supplier_code_counter
    (
        id           TINYINT      NOT NULL CONSTRAINT PK_supplier_code_counter PRIMARY KEY,
        last_value   BIGINT       NOT NULL
    );
END;
GO

-- ── 2. Seed the counter from the current highest supplier code ──
--      If the table already has a row (re-run), leave it untouched.

IF NOT EXISTS (SELECT 1 FROM dbo.supplier_code_counter WHERE id = 1)
BEGIN
    DECLARE @seed BIGINT;

    SELECT @seed = ISNULL(
        (
            SELECT MAX(CAST(SUBSTRING(supplier_code, 5, 10) AS BIGINT))
            FROM   dbo.master_supplier
            WHERE  supplier_code LIKE 'SUP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
        ),
        0   -- no well-formed codes yet → start at 0 so first code = SUP-0000000001
    );

    INSERT INTO dbo.supplier_code_counter (id, last_value)
    VALUES (1, @seed);
END;
GO

-- ── 3. Verification ─────────────────────────────────────────────

SELECT
    last_value                                          AS current_counter,
    'SUP-' + RIGHT('0000000000' + CAST(last_value + 1 AS NVARCHAR(20)), 10)
                                                        AS next_code_will_be
FROM dbo.supplier_code_counter
WHERE id = 1;
GO
