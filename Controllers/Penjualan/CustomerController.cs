using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Penjualan
{
    // Controller untuk Master data pelanggan untuk module penjualan
    public class CustomerController : Controller
    {
        [HttpGet("api/customer")]
        public IActionResult GetCustomerData()
        {
            // Data Dummy
            List<Customer> customersList = new()
            {
                new Customer
                {
                    Kode = "CUST-001",
                    Nama = "PT Maju Bersama",
                    Email = "info@majubersama.co.id",
                    Telepon = "021-5551234",
                    Alamat = "Jl. Sudirman No. 12, Jakarta Pusat",
                    Status = "Aktif"
                },
                new Customer
                {
                     Kode = "CUST-002",
                     Nama = "CV Sinar Terang",
                     Email = "sinarterang@gmail.com",
                     Telepon = "031-7778888",
                     Alamat = "Jl. Pemuda No. 45, Surabaya",
                     Status = "Aktif"
                },
                new Customer
                {
                     Kode: "CUST-003",
                     Nama: "PT Karya Mandiri",
                     Email: "karyamandiri@gmail.com",
                     Telepon: "022-3339999",
                     Alamat: "Jl. Asia Afrika No. 77, Bandung",
                     Status: "Non-aktif",
                }
            };

            return Ok(customersList);
        }
    }
}
