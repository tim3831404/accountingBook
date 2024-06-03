using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services;
using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : Controller
    {
        private readonly IUpdateStockService _updateStockService;

        public StockController(IUpdateStockService updateStockService)
        {
            _updateStockService = updateStockService;
        }

        [HttpGet("StockRawData")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetStockTransactions(DateTime startDate, DateTime endDate, String name)
        {
            var stockInfo = await _updateStockService.GetStockTransactions(startDate, endDate, name);
            return Ok(stockInfo);
        }

        [HttpGet("StockAllInfo")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetAllStockStockBlanceProfit(string name, string stockCode)
        {
            var stockInfo = await _updateStockService.CalculateBalanceProfit(name, stockCode);
            return Ok(stockInfo);
        }

        [HttpGet("Inventory")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetAllStockInventorytions(string name, string stockCode)
        {
            var stockInfo = await _updateStockService.SumStockInventoryAsync(name, stockCode);
            return Ok(stockInfo);
        }

        [HttpGet("RealizedProfit")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetRealizedProfit(string name, string stockCode, DateTime startDate, DateTime endDate)
        {
            var stockInfo = await _updateStockService.SortRealizedProfitAsync(name, stockCode, startDate, endDate);

            return Ok(stockInfo);
        }

        //[HttpGet("QueryDividend")]
        //public async Task<ActionResult<List<Dictionary<string, string>>>> GetDividendInfo(DateTime startDate, DateTime endDate, string stockCode)
        //{
        //    var res = _updateStockService.GetDividendAsync(startDate, endDate, stockCode);
        //    return await res;
        //}

        [HttpPost("RealizedDividend")]
        public async Task<List<Dividends>> GetRealizedDividend(string name, string stockCode)
        {
            var res = _updateStockService.CaculateDividendAsync(name, stockCode);
            return await res;
        }

        [HttpGet("SortDividend")]
        public async Task<IEnumerable<Dividends>> GetSortDividend(string name, string stockCode, int year)
        {
            var res = _updateStockService.SortDividendAsync(name, stockCode, year);
            return await res;
        }
    }
}