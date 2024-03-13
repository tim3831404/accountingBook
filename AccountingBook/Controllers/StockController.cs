using AccountingBook.Interfaces;
using AccountingBook.Models;
using AccountingBook.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : Controller
    {
        private readonly StockService _stockService;
        private readonly IStockRepository _stockRepository;

        public StockController(StockService stockService,
                                IStockRepository stockRepository)
        {
            _stockService = stockService;
            _stockRepository = stockRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stocks>>> GetAllStocks()
        {
            try
            {
                var StockService = await _stockRepository.GetAllStocksAsync();
                return Ok(StockService);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var result = _stockService.GetClosingPriceForStock(id);
            if (result == null)
            {
                return NotFound("找不到資源");
            }

            return Ok(result);
        }
    }
}