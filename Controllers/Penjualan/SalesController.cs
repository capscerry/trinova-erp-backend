using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesQuotationUsecase _salesQuotationUsecase;
        private readonly ISalesCategoryUsecase _salesCategoryUsecase;
        private readonly ISalesOrderUsecase _salesOrderUsecase;

        public SalesController(
            ISalesQuotationUsecase salesQuotationUsecase,
            ISalesCategoryUsecase salesCategoryUsecase,
            ISalesOrderUsecase salesOrderUsecase
        )
        {
            _salesQuotationUsecase = salesQuotationUsecase;
            _salesCategoryUsecase = salesCategoryUsecase;
            _salesOrderUsecase = salesOrderUsecase;
        }

        [HttpPost("/api/SalesQuotation")]
        public async Task<IActionResult> InsertQuotation(
            [FromBody] SalesQuotation quotation
        )
        {
            try
            {
                if (quotation == null)
                {
                    return BadRequest(new
                    {
                        message = "Quotation payload cannot be null"
                    });
                }

                string result = await _salesQuotationUsecase
                    .InsertSalesQuotation(quotation);

                return Ok(new
                {
                    success = true,
                    message = "Berhasil membuat penawaran penjualan",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("/api/SalesQuotation")]
        public async Task<IActionResult> GetAllSalesQuotation()
        {
            try
            {
                var result = await _salesQuotationUsecase.GetAllQuotations();

                return Ok(new
                {
                    success = true,
                    message = "Data Sales Quotation berhasil diambil",
                    data = result ?? new List<QuotationHeaderDTO>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("/api/header-detail/{quotationId}")]
        public async Task<IActionResult> GetQuotationHeaderDetailById(int quotationId)
        {
            var result = await _salesQuotationUsecase
                .GetQuotationHeaderDetailById(quotationId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Sales Quotation tidak ditemukan"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Detail Sales Quotation berhasil diambil",
                data = result
            });
        }

        [HttpGet("/api/SalesQuotation/{customerId}")]
        public async Task<IActionResult> GetAllSalesQuotationById(int customerId)
        {
            try
            {
                var result = await _salesQuotationUsecase.GetAllQuotationById(customerId);
                return Ok(new
                {
                    success = true,
                    message = "Data Sales Quotation Berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("/api/quotation-detail/{quotationId}")]
        public async Task<IActionResult> GetAllQuotationDetailById(int quotationId)
        {
            try
            {
                var result = await _salesQuotationUsecase.GetAllQuotationDetailById(quotationId);
                return Ok(new
                {
                    success = true,
                    message = "Data Detail Quotation Berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("/api/sales-category")]
        public async Task<IActionResult> InsertCategorySales([FromBody] SalesCategory data)
        {
            var result = await _salesCategoryUsecase.InsertDataCategory(data);
            return Ok(result);
        }

        [HttpPost("/api/sales-category/{id}/status")]
        public async Task<IActionResult> UpdateStatusCategory(int id, [FromBody] CategoryStatusRequest request)
        {
            var status = request.IsActive ? 1 : 0;
            var result = await _salesCategoryUsecase.UpdateStatusCategory(id, status);
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

        [HttpPut("/api/sales-category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] SalesCategory model)
        {
            model.Id = id;
            var result = await _salesCategoryUsecase.UpdateDataCategory(model);
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

        [HttpGet("/api/sales-category")]
        public async Task<IActionResult> GetAllCategory()
        {
            var result = await _salesCategoryUsecase.GetAllCategory();
            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),  // array kosong
                    message = "No Category Found"
                });
            }
            return Ok(new
            {
                status = true,
                data = result

            });

        }
        [HttpGet("/api/sales-order")]
        public async Task<IActionResult> GetAllSalesOrder()
        {
            try
            {
                var result = await _salesOrderUsecase.GetAllSalesOrder();

                return Ok(new
                {
                    success = true,
                    message = "Data Sales Order berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        // SALES ORDER CONTROLER 

        [HttpPost("/api/sales-order")]  
        public async Task<IActionResult> PopulateSalesOrder([FromBody] SalesOrderRequest data)
        {
            try
                {
                if (data == null)   
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Request tidak boleh kosong"
                    });
                }

                if (data.Header == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Header sales order wajib diisi"
                    });
                }

                if (data.Detail == null || !data.Detail.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Detail sales order wajib diisi"
                    });
                }

                var result = await _salesOrderUsecase.InsertSalesOrder(data);

                return Ok(new
                {
                    success = true,
                    message = "Sales Order berhasil dibuat",
                    data = new
                    {
                        header = result.Header,
                        detail = result.Detail
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/sales-order/{orderId}")]
        public async Task<IActionResult> GetSalesOrderDetail(int orderId)
        {
            var result = await _salesOrderUsecase.GetSalesOrderDetail(orderId);

            if (result == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Sales order tidak ditemukan"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPatch("/api/sales-order/{id}/cancel")]
        public async Task<IActionResult> CancelSalesOrder(int id)
        {
            try
            {
                await _salesOrderUsecase.CancelSalesOrder(id);

                return Ok(new
                {
                    success = true,
                    message = "Sales Order berhasil dibatalkan, reservasi stok yang belum dikirim sudah dilepas"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/sales-order/by-customer/{customerId}")]
        public async Task<IActionResult> GetSalesOrderByCustId(int customerId)
        {
            var result = await _salesOrderUsecase.GetSalesOrderByCustomerId(customerId);

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "Data Sales Order tidak ditemukan untuk customer ini",
                    data = Array.Empty<object>()
                });
            }

            var data = result.Select(so => new
            {
                id = so.OrderId,
                soNumber = so.SoNumber,
                soDate = so.SoDate,
                tanggalKirim = so.TanggalKirim,
                customerName = so.CustomerName,
                poNumber = so.PoNumber,
                address = so.Address,
                notes = so.Notes,
                subTotal = so.SubTotal ?? 0,
                isTaxAble = so.IsTaxAble ?? false,
                isTaxIncluded = so.IsTaxIncluded,
                taxTotal = so.TaxTotal ?? 0,
                status = so.Status ?? "Draft"
            });

            return Ok(new
            {
                success = true,
                message = "Data Sales Order Berhasil diambil",
                data
            });
        }


    }
}
