using AccountingBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : Controller
    {
        private readonly StockService _stockService;
        public StockController(StockService stockService)
        {
            _stockService = stockService;
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
