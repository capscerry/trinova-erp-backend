using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // MASTER
        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<SupplierCategory> SupplierCategories { get; set; }

        // TRANSACTION
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Supplier>()
                .ToTable("master_supplier");

            modelBuilder.Entity<SupplierCategory>()
                .ToTable("supplier_category");

            modelBuilder.Entity<PurchaseOrder>()
                .ToTable("purchase_order");

            modelBuilder.Entity<PurchaseOrderDetail>()
                .ToTable("purchase_order_detail");

            modelBuilder.Entity<GoodsReceipt>()
                .ToTable("goods_receipt");
        }
    }
}