using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Models.Persediaan.DTO;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class StockTransferUsecase
    {
        private readonly InventoryStockRepo _stockRepo;
        private readonly StockMovementRepo _movementRepo;

        public StockTransferUsecase(
            InventoryStockRepo stockRepo,
            StockMovementRepo movementRepo)
        {
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
        }

        public async Task Transfer(
            StockTransferRequest request)
        {
            var source =
                await _stockRepo.GetByProductWarehouseAsync(
                    request.product_id,
                    request.source_warehouse_id);

            if (source == null)
                throw new Exception("Source stock not found");

            if (source.qty_available < request.quantity)
                throw new Exception("Insufficient stock");

            source.qty_on_hand -= request.quantity;
            source.qty_available -= request.quantity;

            await _stockRepo.UpdateAsync(source);

            var destination =
                await _stockRepo.GetByProductWarehouseAsync(
                    request.product_id,
                    request.destination_warehouse_id);

            if (destination == null)
            {
                destination =
                    new InventoryStock
                    {
                        product_id = request.product_id,
                        warehouse_id = request.destination_warehouse_id,
                        qty_on_hand = request.quantity,
                        qty_reserved = 0,
                        qty_available = request.quantity
                    };

                await _stockRepo.CreateAsync(destination);
            }
            else
            {
                destination.qty_on_hand += request.quantity;
                destination.qty_available += request.quantity;

                await _stockRepo.UpdateAsync(destination);
            }

            await _movementRepo.InsertAsync(
                new StockMovement
                {
                    product_id = request.product_id,
                    movement_type = "TRANSFER",
                    quantity = request.quantity,
                    notes = request.notes,
                    movement_date = DateTime.Now,
                    created_at = DateTime.Now,
                    created_by = request.created_by,
                    source_warehouse_id = request.source_warehouse_id,
                    destination_warehouse_id = request.destination_warehouse_id,
                    reference_number =
                        $"TRF-{DateTime.Now:yyyyMMddHHmmss}"
                });
        }
        public async Task<List<StockMovement>>GetAllAsync()
        {
            return await _movementRepo.GetAllAsync();
        }
    }
}