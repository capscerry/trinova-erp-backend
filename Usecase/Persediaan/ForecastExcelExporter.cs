using ClosedXML.Excel;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class ForecastExcelExporter
    {
        public byte[] Export(List<ForecastResult> forecasts)
        {
            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Summary");
            var forecastSheet = workbook.Worksheets.Add("Forecast Data");

            // =====================================================
            // Calculate Summary
            // =====================================================

            var totalProducts = forecasts.Count;

            var totalForecastQty = forecasts.Sum(x => x.ForecastNextMonth);

            var averageForecast = forecasts.Any()
                ? forecasts.Average(x => x.ForecastNextMonth)
                : 0;

            var forecastMonth = forecasts.FirstOrDefault()?.ForecastMonth ?? "-";

            var highestForecastProduct = forecasts
                .OrderByDescending(x => x.ForecastNextMonth)
                .FirstOrDefault();

            var highestProductName = highestForecastProduct?.ProductName ?? "-";

            var highestProductQty = highestForecastProduct?.ForecastNextMonth ?? 0;

            var forecastMonthDisplay = "-";

            if (DateTime.TryParse($"{forecastMonth}-01", out var forecastDate))
            {
                forecastMonthDisplay = forecastDate.ToString("MMMM yyyy");
            }

            // =====================================================
            // SUMMARY SHEET
            // =====================================================

            summarySheet.Range("A1:B1").Merge();

            summarySheet.Cell("A1").Value = "Demand Forecast Summary";
            summarySheet.Cell("A1").Style.Font.Bold = true;
            summarySheet.Cell("A1").Style.Font.FontSize = 16;
            summarySheet.Cell("A1").Style.Font.FontColor = XLColor.White;
            summarySheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.DarkBlue;
            summarySheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            summarySheet.Cell("A3").Value = "Generated At";
            summarySheet.Cell("B3").Value = DateTime.UtcNow.AddHours(7);

            summarySheet.Cell("A4").Value = "Forecast Month";
            summarySheet.Cell("B4").Value = forecastMonthDisplay;

            summarySheet.Cell("A5").Value = "Total Products";
            summarySheet.Cell("B5").Value = totalProducts;

            summarySheet.Cell("A6").Value = "Total Forecast Quantity";
            summarySheet.Cell("B6").Value = totalForecastQty;

            summarySheet.Cell("A7").Value = "Highest Forecast Product";
            summarySheet.Cell("B7").Value = highestProductName;

            summarySheet.Cell("A8").Value = "Highest Forecast Quantity";
            summarySheet.Cell("B8").Value = highestProductQty;

            summarySheet.Cell("A9").Value = "Average Forecast Quantity";
            summarySheet.Cell("B9").Value = averageForecast;

            summarySheet.Range("A3:A9").Style.Font.Bold = true;

            summarySheet.Range("A3:B9").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summarySheet.Range("A3:B9").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            summarySheet.Cell("B3").Style.DateFormat.Format = "dd MMM yyyy HH:mm";
            summarySheet.Cell("B6").Style.NumberFormat.Format = "#,##0";
            summarySheet.Cell("B8").Style.NumberFormat.Format = "#,##0";
            summarySheet.Cell("B9").Style.NumberFormat.Format = "#,##0.00";

            summarySheet.Columns().AdjustToContents();

            // =====================================================
            // FORECAST DATA HEADER
            // =====================================================

            forecastSheet.Cell("A1").Value = "Product ID";
            forecastSheet.Cell("B1").Value = "Product Name";
            forecastSheet.Cell("C1").Value = "Forecast Month";
            forecastSheet.Cell("D1").Value = "Forecast Quantity";
            forecastSheet.Cell("E1").Value = "Historical Records";
            forecastSheet.Cell("F1").Value = "Last Training Period";
            forecastSheet.Cell("G1").Value = "Generated At";

            var headerRange = forecastSheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // =====================================================
            // FORECAST DATA CONTENT
            // =====================================================

            var row = 2;

            foreach (var forecast in forecasts)
            {
                var forecastMonthText = forecast.ForecastMonth;

                if (DateTime.TryParse($"{forecast.ForecastMonth}-01", out var forecastMonthDate))
                {
                    forecastMonthText = forecastMonthDate.ToString("MMMM yyyy");
                }

                var trainingPeriodText = forecast.LastTrainingPeriod;

                if (DateTime.TryParse($"{forecast.LastTrainingPeriod}-01", out var trainingDate))
                {
                    trainingPeriodText = trainingDate.ToString("MMMM yyyy");
                }

                forecastSheet.Cell(row, 1).Value = forecast.ProductId;
                forecastSheet.Cell(row, 2).Value = forecast.ProductName;
                forecastSheet.Cell(row, 3).Value = forecastMonthText;
                forecastSheet.Cell(row, 4).Value = forecast.ForecastNextMonth;
                forecastSheet.Cell(row, 5).Value = forecast.HistoricalRecords;
                forecastSheet.Cell(row, 6).Value = trainingPeriodText;
                forecastSheet.Cell(row, 7).Value = forecast.GeneratedAt;

                row++;
            }

            // =====================================================
            // FORECAST DATA FORMATTING
            // =====================================================

            forecastSheet.Column(4).Style.NumberFormat.Format = "#,##0.00";

            forecastSheet.SheetView.FreezeRows(1);

            forecastSheet.RangeUsed().SetAutoFilter();

            forecastSheet.Columns().AdjustToContents();

            // =====================================================
            // SAVE WORKBOOK
            // =====================================================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}
