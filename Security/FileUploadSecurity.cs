using System.IO.Compression;
using System.Text;

using ExcelDataReader;

namespace trinova_erp_backend.Security
{
    /// <summary>
    /// Centralized file-upload validation shared by every endpoint that accepts
    /// a file (Excel catalog import, CSV training upload). Performs extension
    /// allow-listing, declared Content-Type allow-listing, magic-byte (file
    /// signature) verification, size limits, and — for CSV — a formula/CSVinjection scan.
    ///
    /// This is a signature/heuristic layer, not a substitute for a real
    /// anti-malware engine. It is designed to reject the common cases of an
    /// upload lying about its own type (e.g. an .exe renamed to .xlsx) or
    /// containing content that is dangerous to open downstream (e.g. an Excel
    /// formula-injection payload inside a CSV).
    /// </summary>
    public static class FileUploadSecurity
    {
        public sealed record FileValidationResult(bool IsValid, string? ErrorMessage, int StatusCode = 400)
        {
            public static FileValidationResult Success() => new(true, null);

            public static FileValidationResult Fail(string message, int statusCode = 400) =>
                new(false, message, statusCode);
        }

        // ─── Known file signatures ("magic bytes") ─────────────────────────────

        private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };
        private static readonly byte[] OleSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        // Signatures that must never appear at the start of a CSV/plain-text
        // upload. Their presence means the content is not really text, no
        // matter what extension or Content-Type the client claims.
        private static readonly (byte[] Signature, string Description)[] BlockedTextSignatures =
        {
            (new byte[] { 0x4D, 0x5A },                                     "Windows executable (MZ header)"),
            (new byte[] { 0x7F, 0x45, 0x4C, 0x46 },                         "ELF executable"),
            (new byte[] { 0x50, 0x4B, 0x03, 0x04 },                         "ZIP/Office archive"),
            (new byte[] { 0x25, 0x50, 0x44, 0x46 },                         "PDF document"),
            (new byte[] { 0x89, 0x50, 0x4E, 0x47 },                         "PNG image"),
            (new byte[] { 0xFF, 0xD8, 0xFF },                               "JPEG image"),
            (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "OLE compound document"),
        };

        private static readonly string[] ExcelAllowedContentTypes =
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
            "application/vnd.ms-excel",                                          // .xls
            "application/octet-stream",                                         // some browsers send this for .xls
        };

        private static readonly string[] CsvAllowedContentTypes =
        {
            "text/csv",
            "application/vnd.ms-excel", // Excel sometimes labels a saved .csv this way
            "text/plain",
            "application/octet-stream",
        };

        // Characters that Excel/Google Sheets treat as the start of a formula
        // when they are the first character of a cell — the classic CSV/formula
        // injection vector (e.g. "=CMD|'/c calc'!A1").
        private static readonly char[] FormulaInjectionPrefixes = { '=', '+', '-', '@' };

        // Substrings that indicate embedded script/markup content inside a
        // cell value — a stored-XSS payload smuggled through otherwise
        // well-formed spreadsheet/CSV data. Matched case-insensitively.
        private static readonly string[] SuspiciousContentPatterns =
        {
            "<script", "javascript:", "vbscript:", "onerror=", "onload=",
            "onclick=", "<iframe", "<object", "<embed", "data:text/html",
        };

        // ExcelDataReader needs this registered to read legacy .xls code
        // pages. Runs once per process via the static field initializer.
        private static readonly bool _codePagesRegistered = RegisterCodePagesOnce();

        private static bool RegisterCodePagesOnce()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return true;
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        public static FileValidationResult ValidateExcel(IFormFile? file, long maxSizeBytes)
        {
            var basic = ValidateBasics(file, maxSizeBytes, out var ext);
            if (basic != null) return basic;

            if (ext != ".xlsx" && ext != ".xls")
                return FileValidationResult.Fail("Hanya file .xlsx atau .xls yang diperbolehkan.");

            if (!string.IsNullOrEmpty(file!.ContentType) &&
                !ExcelAllowedContentTypes.Contains(file.ContentType))
            {
                return FileValidationResult.Fail(
                    $"Content-Type '{file.ContentType}' tidak sesuai dengan format Excel.");
            }

            var header = ReadHeaderBytes(file, 8);
            var isZip = header.Length >= 4 && header.AsSpan(0, 4).SequenceEqual(ZipSignature);
            var isOle = header.Length >= 8 && header.AsSpan(0, 8).SequenceEqual(OleSignature);

            if (ext == ".xlsx" && !isZip)
            {
                return FileValidationResult.Fail(
                    "Isi file tidak cocok dengan format .xlsx (magic byte tidak sesuai). " +
                    "File kemungkinan telah diganti namanya dari format lain.");
            }

            if (ext == ".xls" && !isOle)
            {
                return FileValidationResult.Fail(
                    "Isi file tidak cocok dengan format .xls (magic byte tidak sesuai). " +
                    "File kemungkinan telah diganti namanya dari format lain.");
            }

            var suspiciousPattern = ScanExcelForSuspiciousContent(file);
            if (suspiciousPattern != null)
            {
                return FileValidationResult.Fail(
                    $"File Excel mengandung konten yang berpotensi berbahaya ('{suspiciousPattern}' " +
                    "terdeteksi di salah satu cell). Ini mengindikasikan upaya script/XSS injection " +
                    "melalui data yang diimpor.");
            }

            var macroFinding = ScanExcelForMacroContent(file);
            if (macroFinding != null)
            {
                return FileValidationResult.Fail(
                    $"File Excel terdeteksi mengandung macro/VBA project ('{macroFinding}'). " +
                    "File macro-enabled (mis. .xlsm yang disamarkan sebagai .xlsx) tidak diperbolehkan " +
                    "karena berpotensi menjalankan kode berbahaya saat dibuka di Microsoft Excel.");
            }

            return FileValidationResult.Success();
        }

        public static FileValidationResult ValidateCsv(IFormFile? file, long maxSizeBytes)
        {
            var basic = ValidateBasics(file, maxSizeBytes, out var ext);
            if (basic != null) return basic;

            if (ext != ".csv")
                return FileValidationResult.Fail("Hanya file .csv yang diperbolehkan.");

            if (!string.IsNullOrEmpty(file!.ContentType) &&
                !CsvAllowedContentTypes.Contains(file.ContentType))
            {
                return FileValidationResult.Fail(
                    $"Content-Type '{file.ContentType}' tidak sesuai dengan format CSV.");
            }

            var header = ReadHeaderBytes(file, 8);
            foreach (var (signature, description) in BlockedTextSignatures)
            {
                if (header.Length >= signature.Length &&
                    header.AsSpan(0, signature.Length).SequenceEqual(signature))
                {
                    return FileValidationResult.Fail(
                        $"File terdeteksi sebagai {description}, bukan teks CSV biasa.");
                }
            }

            string content;
            try
            {
                content = ReadAllTextStrict(file);
            }
            catch (DecoderFallbackException)
            {
                return FileValidationResult.Fail("File tidak dapat dibaca sebagai teks UTF-8 yang valid.");
            }

            var injectedChar = ScanForFormulaInjection(content);
            if (injectedChar != null)
            {
                return FileValidationResult.Fail(
                    $"File CSV mengandung karakter formula ('{injectedChar}') di awal salah satu cell, " +
                    "yang berpotensi CSV/formula injection. Hapus karakter tersebut dan unggah ulang.");
            }

            var suspiciousPattern = ScanForSuspiciousMarkup(content);
            if (suspiciousPattern != null)
            {
                return FileValidationResult.Fail(
                    $"File CSV mengandung konten yang berpotensi berbahaya ('{suspiciousPattern}' " +
                    "terdeteksi). Ini mengindikasikan upaya script/XSS injection melalui data yang diimpor.");
            }

            return FileValidationResult.Success();
        }

        // ─── Internal helpers ───────────────────────────────────────────────────

        private static FileValidationResult? ValidateBasics(IFormFile? file, long maxSizeBytes, out string extension)
        {
            extension = string.Empty;

            if (file == null || file.Length == 0)
                return FileValidationResult.Fail("File tidak ditemukan atau kosong.");

            if (file.Length > maxSizeBytes)
            {
                return FileValidationResult.Fail(
                    $"Ukuran file ({file.Length / 1024.0 / 1024.0:F2} MB) melebihi batas maksimum " +
                    $"{maxSizeBytes / 1024.0 / 1024.0:F0} MB.",
                    statusCode: 413);
            }

            extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return null;
        }

        private static byte[] ReadHeaderBytes(IFormFile file, int count)
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[count];
            var totalRead = 0;
            int read;
            while (totalRead < count && (read = stream.Read(buffer, totalRead, count - totalRead)) > 0)
                totalRead += read;

            return totalRead == count ? buffer : buffer[..totalRead];
        }

        private static string ReadAllTextStrict(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        private static string? ScanForFormulaInjection(string content)
        {
            // Cap how many lines we scan so a very large legitimate file can't
            // be used to stall the request — the header and first data rows
            // are enough to catch an injected formula in practice.
            const int maxLinesToScan = 5000;

            var lines = content.Split('\n');
            var limit = Math.Min(lines.Length, maxLinesToScan);

            for (var i = 0; i < limit; i++)
            {
                foreach (var rawCell in lines[i].Split(','))
                {
                    var cell = rawCell.Trim().Trim('"');
                    if (cell.Length == 0) continue;

                    if (Array.IndexOf(FormulaInjectionPrefixes, cell[0]) >= 0)
                        return cell[0].ToString();
                }
            }

            return null;
        }

        private static string? ScanForSuspiciousMarkup(string content)
        {
            var lower = content.ToLowerInvariant();
            foreach (var pattern in SuspiciousContentPatterns)
            {
                if (lower.Contains(pattern))
                    return pattern;
            }

            return null;
        }

        private static string? ScanExcelForSuspiciousContent(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);

                do
                {
                    while (reader.Read())
                    {
                        for (var col = 0; col < reader.FieldCount; col++)
                        {
                            var value = reader.GetValue(col)?.ToString();
                            if (string.IsNullOrEmpty(value)) continue;

                            var lower = value.ToLowerInvariant();
                            foreach (var pattern in SuspiciousContentPatterns)
                            {
                                if (lower.Contains(pattern))
                                    return pattern;
                            }
                        }
                    }
                } while (reader.NextResult());
            }
            catch
            {
                // If the workbook can't be parsed here, the magic-byte check
                // above already caught a mismatched format; let the caller's
                // own parse attempt surface a clearer error instead of
                // failing the validation step itself on a parse exception.
                return null;
            }

            return null;
        }

        // An .xlsx is a ZIP archive of OOXML parts. A macro-enabled workbook
        // (normally saved as .xlsm) adds an "xl/vbaProject.bin" part holding
        // the compiled VBA project. Renaming a .xlsm to .xlsx does not remove
        // that part, so checking for its presence catches the disguise even
        // though the magic bytes (PK..) are identical for both formats.
        private static string? ScanExcelForMacroContent(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

                foreach (var entry in archive.Entries)
                {
                    var name = entry.FullName.Replace('\\', '/').ToLowerInvariant();
                    if (name.Contains("vbaproject.bin"))
                        return entry.FullName;
                }
            }
            catch
            {
                // Not a valid ZIP archive at all — the magic-byte check above
                // already handles that case; nothing further to flag here.
                return null;
            }

            return null;
        }
    }
}
