using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Models.Persediaan.DTO;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class StockTransferUsecase
    {
        private readonly InventoryStockRepo _stockRepo;
        private readonly StockMovementRepo _movementRepo;

        private readonly StockTransactionRepo _transactionRepo;

        public StockTransferUsecase(
            InventoryStockRepo stockRepo,
            StockMovementRepo movementRepo,
            StockTransactionRepo transactionRepo)
        {
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
            _transactionRepo = transactionRepo;
        }

        public async Task<string> GetNextTRFNumber()
        {
            return await _movementRepo.GenerateTRFNumber();
        }

        public async Task Transfer(
            StockTransferRequest request)
        {   
            var status = "CREATED";

            var referenceNumber =
                await _movementRepo.GenerateTRFNumber();

            if (request.source_warehouse_id == request.destination_warehouse_id)
            {
                throw new Exception("Source and destination warehouse cannot be the same.");
            }

            if (request.quantity <= 0)
            {
                throw new Exception("Quantity must be greater than zero.");
            }

            var source =
                await _stockRepo.GetByProductWarehouseAsync(
                    request.product_id,
                    request.source_warehouse_id);

            if (source == null)
                throw new Exception("Source stock not found");

            if (source.qty_available < request.quantity)
                throw new Exception("Insufficient stock");

            if (status == "PROCESSED" || status == "COMPLETED")
            {
                await _stockRepo.SyncToSupplierProductsAsync(request.product_id);
            }

            await _movementRepo.InsertAsync(
                new StockMovement
                {
                    product_id = request.product_id,
                    movement_type = "TRANSFER",
                    quantity = request.quantity,
                    notes = request.notes,
                    movement_date = DateTime.UtcNow.AddHours(7),
                    created_at = DateTime.UtcNow.AddHours(7),
                    created_by = request.created_by,
                    source_warehouse_id = request.source_warehouse_id,
                    destination_warehouse_id = request.destination_warehouse_id,
                    reference_number = referenceNumber,
                    status = status,
                    processed_at = status == "PROCESSED"
                        ? DateTime.UtcNow.AddHours(7)
                        : null,

                    completed_at = status == "COMPLETED"
                        ? DateTime.UtcNow.AddHours(7)
                        : null,

                    canceled_at = null,
                });
        }

        public async Task ProcessAsync(int movementId)
        {
            var movement =
                await _movementRepo.GetByIdAsync(movementId);

            if (movement == null)
                throw new Exception("Transfer not found.");

            if (movement.status != "CREATED")
                throw new Exception("Only CREATED transfer can be processed.");

            var source =
                await _stockRepo.GetByProductWarehouseAsync(
                    movement.product_id,
                    movement.source_warehouse_id!.Value);

            if (source == null)
                throw new Exception("Source stock not found.");

            if (source.qty_available < movement.quantity)
                throw new Exception("Insufficient stock.");

            source.qty_on_hand -= movement.quantity;
            source.qty_available -= movement.quantity;

            await _stockRepo.UpdateAsync(source);

            await _stockRepo.SyncToSupplierProductsAsync(
                movement.product_id);

            await _transactionRepo.CreateAsync(
                new StockTransaction
                {
                    product_id = movement.product_id,
                    warehouse_id = movement.source_warehouse_id!.Value,
                    transaction_type = "TRANSFER_OUT",
                    quantity = movement.quantity,
                    reference_no = movement.reference_number,
                    reference_module = "Stock Transfer",
                    reference_id = movement.movement_id,
                    remarks = "Stock transferred to destination warehouse",
                    created_at = DateTime.UtcNow.AddHours(7)
                }
            );
            await _movementRepo.UpdateStatusAsync(
                movementId,
                "PROCESSED");
        }

        public async Task CompleteAsync(int movementId)
        {
            var movement =
                await _movementRepo.GetByIdAsync(movementId);

            if (movement == null)
                throw new Exception("Transfer not found.");

            if (movement.status != "PROCESSED")
                throw new Exception("Only PROCESSED transfer can be completed.");

            var destination =
                await _stockRepo.GetByProductWarehouseAsync(
                    movement.product_id,
                    movement.destination_warehouse_id!.Value);

            if (destination == null)
            {
                destination = new InventoryStock
                {
                    product_id = movement.product_id,
                    warehouse_id = movement.destination_warehouse_id.Value,
                    qty_on_hand = movement.quantity,
                    qty_reserved = 0,
                    qty_available = movement.quantity
                };

                await _stockRepo.CreateAsync(destination);
            }
            else
            {
                destination.qty_on_hand += movement.quantity;
                destination.qty_available += movement.quantity;

                await _stockRepo.UpdateAsync(destination);
            }

            await _transactionRepo.CreateAsync(
                new StockTransaction
                {
                    product_id = movement.product_id,
                    warehouse_id = movement.destination_warehouse_id!.Value,
                    transaction_type = "TRANSFER_IN",
                    quantity = movement.quantity,
                    reference_no = movement.reference_number,
                    reference_module = "Stock Transfer",
                    reference_id = movement.movement_id,
                    remarks = "Stock received from source warehouse",
                    created_at = DateTime.UtcNow.AddHours(7)
                }
            );

            await _stockRepo.SyncToSupplierProductsAsync(
                movement.product_id);

            await _movementRepo.UpdateStatusAsync(
                movementId,
                "COMPLETED");
        }

        public async Task CancelAsync(int movementId)
        {
            var movement =
                await _movementRepo.GetByIdAsync(movementId);

            if (movement == null)
                throw new Exception("Transfer not found.");

            if (movement.status != "CREATED")
                throw new Exception("Only CREATED transfer can be canceled.");

            await _movementRepo.UpdateStatusAsync(
                movementId,
                "CANCELED");
        }

        public async Task<List<StockMovement>> GetAllAsync()
        {
            return await _movementRepo
                .GetByMovementTypeAsync(
                    "TRANSFER");
        }

        public async Task<StockMovement?> GetDetail(int id)
        {
            return await _movementRepo.GetByIdAsync(id);
        }
    }
}