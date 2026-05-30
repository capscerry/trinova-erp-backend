using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class DemandForecastUsecase
    {
        private readonly ForecastRepo _repo;

        public DemandForecastUsecase(
            ForecastRepo repo
        )
        {
            _repo = repo;
        }

        public async Task<List<ForecastResult>>
            GenerateForecast()
        {
            var data =
                await _repo.GetForecastData();

            foreach (var item in data)
            {
                item.forecast_next_month =
                    Math.Ceiling(
                        item.total_usage * 1.10m
                    );

                if (
                    item.current_stock
                    >= item.forecast_next_month
                )
                {
                    item.recommendation =
                        "Stock Aman";
                }
                else if (
                    item.current_stock
                    >= item.forecast_next_month * 0.5m
                )
                {
                    item.recommendation =
                        "Perlu Reorder";
                }
                else
                {
                    item.recommendation =
                        "Segera Restock";
                }
            }

            return data;
        }
    }
}