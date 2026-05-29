using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Models;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // MASTER
        // =========================

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<SupplierCategory> SupplierCategories { get; set; }

        // INVENTORY MASTER

        public DbSet<MasterProduct> MasterProducts { get; set; }

        public DbSet<MasterProductCategory> MasterProductCategories { get; set; }

        public DbSet<MasterUom> MasterUoms { get; set; }

        public DbSet<MasterWarehouse> MasterWarehouses { get; set; }

        public DbSet<InventoryStock> InventoryStocks { get; set; }
     
        public DbSet<StockTransaction> StockTransactions { get; set; }

        public DbSet<ProductSubcategory> MasterProductSubcategories { get; set; }
        // =========================
        // TRANSACTION
        // =========================

        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // MASTER
            // =========================

            modelBuilder.Entity<Supplier>()
                .ToTable("master_supplier");

            modelBuilder.Entity<SupplierCategory>()
                .ToTable("supplier_category");

            // INVENTORY MASTER

            modelBuilder.Entity<MasterProduct>()
                .ToTable("master_product");

            modelBuilder.Entity<MasterProductCategory>()
                .ToTable("master_product_category");

            modelBuilder.Entity<MasterUom>()
                .ToTable("master_uom");

            modelBuilder.Entity<MasterWarehouse>()
                .ToTable("master_warehouse");

            modelBuilder.Entity<ProductSubcategory>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.category_id);

            modelBuilder.Entity<InventoryStock>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.product_id);

            modelBuilder.Entity<InventoryStock>()
                .HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.warehouse_id);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.product_id);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.warehouse_id);

            // =========================
            // TRANSACTION
            // =========================

            modelBuilder.Entity<PurchaseOrder>()
                .ToTable("purchase_order");

            modelBuilder.Entity<PurchaseOrderDetail>()
                .ToTable("purchase_order_detail");

            modelBuilder.Entity<GoodsReceipt>()
                .ToTable("goods_receipt");

            modelBuilder.Entity<ProductSubcategory>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.category_id);
        }
    }
}