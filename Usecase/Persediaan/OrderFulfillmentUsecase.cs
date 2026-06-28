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

        public async Task Fulfill(
            OrderFulfillmentRequest request)
        {
            Console.WriteLine("========== ORDER FULFILLMENT ==========");

            Console.WriteLine(
                $"PRODUCT_ID = {request.product_id}");

            Console.WriteLine(
                $"WAREHOUSE_ID = {request.warehouse_id}");

            Console.WriteLine(
                $"QTY = {request.quantity}");

            Console.WriteLine("STEP 1 - GET STOCK");

            var stock =
                await _stockRepo.GetByProductWarehouseAsync(
                    request.product_id,
                    request.warehouse_id);

            Console.WriteLine(
                $"STOCK FOUND = {stock != null}");

            if (stock == null)
                throw new Exception("Stock not found");

            Console.WriteLine("STEP 2 - VALIDATE STOCK");

            if (stock.qty_available < request.quantity)
                throw new Exception("Insufficient stock");

            Console.WriteLine("STEP 3 - UPDATE STOCK");

            stock.qty_on_hand -= request.quantity;
            stock.qty_available -= request.quantity;

            await _stockRepo.UpdateAsync(stock);

            Console.WriteLine("STEP 4 - CREATE TRANSACTION");

            await _transactionRepo.CreateAsync(
                new StockTransaction
                {
                    product_id = request.product_id,
                    warehouse_id = request.warehouse_id,
                    transaction_type = "OUT",
                    quantity = request.quantity,
                    reference_no = request.reference_no,
                    reference_module = "ORDER_FULFILLMENT",
                    remarks = request.notes,
                    created_at = DateTime.Now
                });

            Console.WriteLine("STEP 5 - CREATE MOVEMENT");

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
                    reference_number =
                        request.reference_no ??
                        $"FUL-{DateTime.Now:yyyyMMddHHmmss}"
                });

            Console.WriteLine("STEP 6 - DONE");
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