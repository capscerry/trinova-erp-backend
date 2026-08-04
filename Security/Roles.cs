namespace trinova_erp_backend.Security
{

    public static class Roles
    {


        public const string Admin              = "Admin,admin";
        public const string Purchasing         = "Purchasing,purchasing,Pembelian,pembelian";
        public const string ProcurementManager = "Procurement Manager";
        public const string Inventory          = "Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan";
        public const string Sales              = "Penjualan,penjualan,Sales,sales";

        public const string PurchasingAccess =
            Admin + "," + Purchasing + "," + ProcurementManager;

        public const string PurchasingAndInventoryAccess =
            Admin + "," + Purchasing + "," + ProcurementManager + "," + Inventory;

        public const string AllOperationalAccess =
            Admin + "," + Purchasing + "," + ProcurementManager + "," + Inventory + "," + Sales;
    }
}
