using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace trinova_erp_backend.Controllers.Penjualan
{
    public class CustomerController : Controller
    {

        private readonly ICustomerUsecase _customerCategoryUsecase;
        public CustomerController(ICustomerUsecase customerCategoryUsecase)
        {
            _customerCategoryUsecase = customerCategoryUsecase;
        }


        [HttpPost("/api/category-customer")]
        public async Task<IActionResult> InsertCategory([FromBody]CategoryCustomer model)
        {
            var result = await _customerCategoryUsecase.InsertDataCategory(model);
            return Ok(result);
        }


        [HttpPost("api/customer")]
        public async Task<IActionResult> InsertCustomer([FromBody] Customer customer)
        {
            var result = await _customerCategoryUsecase.InsertMasterCustomer(customer);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpPost("/api/category-customer/{id}/status")]
        public async Task<IActionResult> UpdateStatusCategory(int id, [FromBody] CategoryStatusRequest request)
        {
            var status = request.IsActive ? 1 : 0;
            var result = await _customerCategoryUsecase.UpdateStatusCategory(id, status);
            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Data"
            });
        }

        [HttpPut("/api/category-customer/{id}")]
        public async Task<IActionResult> UpdateCategory(int id , [FromBody] CategoryCustomer model)
        {
            model.Id = id;
                var result = await _customerCategoryUsecase.UpdateDataCategory(model);
            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }

            return BadRequest(new { 
                status = false,
                message = "Failed Update Data"
            });

        }

        [HttpGet("/api/category-customer")]
        public async Task<IActionResult> GetAllCategory()
        {
            var result = await _customerCategoryUsecase.GetAllCategory();
            if(result == null || result.Count == 0)
            {
                return BadRequest(new
                {   
                    status = false,
                    data = "No Category Found"
                });
            }
            return Ok(new
            {
                status = true,
                data = result

            });

        }

        //[HttpPost("/api/customer")]
        //public async Task<IActionResult> InsertCustomer(Customer model)
        //{

        //}



        [HttpGet("api/customer")]
        public async Task<IActionResult> GetCustomerData()
        {
            var customerList = await _customerCategoryUsecase.GetAllCustomer();

            return Ok(new
            {
                status = true,
                data = customerList ?? new List<Customer>() // 🔥 jaga-jaga null
            });
        }

        [HttpGet("api/customer/active")]
        public async Task<IActionResult> GetCustomerActive()
        {
            var customerActive = await _customerCategoryUsecase.GetCustomerActive();
            return Ok(new
            {
                status = true,
                data = customerActive ?? new List<Customer>()
            });
        }


        [HttpPut("api/customer/{id}")]
        public async Task<IActionResult> UpdateCustomer(
        int id,
        [FromBody] Customer customer)
        {
            customer.CustomerId = id;

            var result = await _customerCategoryUsecase.UpdateMasterCustomer(customer);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Customer"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Customer"
            });
        }

        [HttpPatch("api/customer/{id}/status")]
        public async Task<IActionResult> ToggleCustomerStatus(int id)
        {
            var result = await _customerCategoryUsecase.ToggleCustomerStatus(id);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Customer Status"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Customer Status"
            });
        }
    }
}
