//using AccountingBook.Interfaces;
//using AccountingBook.Models;
//using AccountingBook.Services;
//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System;

//namespace AccountingBook.Controllers

//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class UpdateClosingPriceController : Controller
//    {
//        private readonly UpdateClosingPriceService _updateClosingPriceService;

//        public UpdateClosingPriceController(UpdateClosingPriceService updateClosingPriceService
//                                )
//        {
//            _updateClosingPriceService = updateClosingPriceService;

//        }

//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<Stocks>>> GetAllStocks()
//        {
//            try
//            {
//                var StockService = await _updateClosingPriceService.UpdateStockPricesAsync();
//                return Ok(StockService);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }
//    }
//}