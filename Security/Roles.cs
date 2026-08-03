namespace trinova_erp_backend.Security
{
    /// <summary>
    /// Centralised role-name constants for use in [Authorize(Roles = ...)] attributes.
    ///
    /// Usage:
    ///   [Authorize(Roles = Roles.PurchasingAccess)]
    ///   [Authorize(Roles = Roles.AdminOnly)]
    ///
    /// Adding a new role that should inherit Purchasing permissions?
    /// Append it to <see cref="PurchasingAccess"/> here — no controller changes needed.
    /// </summary>
    public static class Roles
    {
        // ── Individual roles ──────────────────────────────────────────────────

        public const string Admin              = "Admin,admin";
        public const string Purchasing         = "Purchasing,purchasing,Pembelian,pembelian";
        public const string ProcurementManager = "Procurement Manager";
        public const string Inventory          = "Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan";
        public const string Sales              = "Penjualan,penjualan,Sales,sales";

        // ── Combined access strings ───────────────────────────────────────────

        /// <summary>
        /// All roles that have access to Purchasing / Pembelian module endpoints.
        /// Both "Purchasing" staff and the "Procurement Manager" are included.
        /// </summary>
        public const string PurchasingAccess =
            Admin + "," + Purchasing + "," + ProcurementManager;

        /// <summary>
        /// Purchasing + Procurement Manager + Inventory roles.
        /// Used on shared resources like Purchase Requisitions that span
        /// both the Purchasing and Inventory modules.
        /// </summary>
        public const string PurchasingAndInventoryAccess =
            Admin + "," + Purchasing + "," + ProcurementManager + "," + Inventory;

        /// <summary>
        /// All operational roles (Purchasing, Procurement Manager, Inventory, Sales).
        /// Used on master-data endpoints that every module reads.
        /// </summary>
        public const string AllOperationalAccess =
            Admin + "," + Purchasing + "," + ProcurementManager + "," + Inventory + "," + Sales;
    }
}
