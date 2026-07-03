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
                    movement_date = DateTime.Now,
                    created_at = DateTime.Now,
                    created_by = request.created_by,
                    source_warehouse_id = request.source_warehouse_id,
                    destination_warehouse_id = request.destination_warehouse_id,
                    reference_number = referenceNumber,
                    status = status,
                    processed_at = status == "PROCESSED"
                        ? DateTime.Now
                        : null,

                    completed_at = status == "COMPLETED"
                        ? DateTime.Now
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