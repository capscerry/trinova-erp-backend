namespace trinova_erp_backend.Models.Penjualan
{
    public class Customer
    {
            public int CustomerId { get; set; }
            public string? CustomerName { get; set; }
            public string? CustomerCode { get; set; }
            public string? NoTelpBisnis { get; set; }
            public string? Alamat { get; set; }
            public string? Email { get; set; }

            public DateTime? CreatedDate { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime? UpdateDate { get; set; }
            public string? UpdateBy { get; set; }

            public bool IsActive { get; set; }
            public string? CategoryName { get; set; }

            public int? CategoryId { get; set; }
        
    }
}
