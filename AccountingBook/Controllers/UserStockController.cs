using AccountingBook.Interfaces;
using AccountingBook.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserStockController : Controller
    {
        private readonly IUserStockService _userStockService;

        public UserStockController(IUserStockService userStockService)
        {
            _userStockService = userStockService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserStocks>>> GetAllUserStocks()
        {
            try
            {
                var userStockService = await _userStockService.GetAllUserStocksAsync();
                return Ok(userStockService);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}