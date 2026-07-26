using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Models.Persediaan.DTO;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class OrderFulfillmentUsecase
    {
        private readonly InventoryStockRepo _stockRepo;
        private readonly StockMovementRepo _movementRepo;
        private readonly StockTransactionRepo _transactionRepo;

        public OrderFulfillmentUsecase(
            InventoryStockRepo stockRepo,
            StockMovementRepo movementRepo,
            StockTransactionRepo transactionRepo)
        {
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
            _transactionRepo = transactionRepo;
        }

        public async Task Fulfill(OrderFulfillmentRequest request)
        {
            var referenceNo = $"FUL-{DateTime.Now:yyyyMMddHHmmss}";

            var stock =
                await _stockRepo.GetByProductWarehouseAsync(
                    request.product_id,
                    request.warehouse_id);

            if (stock == null)
                throw new Exception("Stock not found");

            if (stock.qty_available < request.quantity)
                throw new Exception("Insufficient stock");

            stock.qty_on_hand -= request.quantity;
            stock.qty_available -= request.quantity;

            await _stockRepo.UpdateAsync(stock);

            await _transactionRepo.CreateAsync(
                new StockTransaction
                {
                    product_id = request.product_id,
                    warehouse_id = request.warehouse_id,
                    transaction_type = "OUT",
                    quantity = request.quantity,
                    reference_no = referenceNo,
                    reference_module = "ORDER_FULFILLMENT",
                    remarks = request.notes,
                    created_at = DateTime.Now
                }
            );

            await _movementRepo.InsertAsync(
                new StockMovement
                {
                    product_id = request.product_id,
                    movement_type = "OUTBOUND",
                    quantity = request.quantity,
                    notes = request.notes,
                    movement_date = DateTime.Now,
                    created_at = DateTime.Now,
                    created_by = request.created_by,
                    source_warehouse_id = request.warehouse_id,
                    reference_number = referenceNo,

                    status = "PROCESSED",
                    processed_at = DateTime.Now
                }
            );
        }

        public async Task CompleteAsync(int movementId)
        {
            var movement =
                await _movementRepo.GetByIdAsync(movementId);

            if (movement == null)
                throw new Exception("Stock movement not found");

            if (movement.status != "PROCESSED")
                throw new Exception("Only processed movements can be completed");

            await _movementRepo.UpdateStatusAsync(
                movementId,
                "COMPLETED");
        }

        public async Task CancelAsync(int movementId)
        {
            var movement =
                await _movementRepo.GetByIdAsync(movementId);

            if (movement == null)
                throw new Exception("Stock movement not found");

            if (movement.status != "PROCESSED")
                throw new Exception("Only processed movements can be canceled");

            await _movementRepo.UpdateStatusAsync(
                movementId,
                "CANCELED");
        }

        public async Task<List<StockMovement>>
            GetAllAsync()
        {
            return await _movementRepo
                .GetByMovementTypeAsync(
                    "OUTBOUND");
        }
    }
}