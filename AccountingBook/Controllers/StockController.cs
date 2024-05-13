using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services;
using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : Controller
    {
        private readonly IStockTransactionsRepository _stockTransactionsRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUpdateStockService _updateStockService;

        public StockController(IStockTransactionsRepository stockTransactionsRepository,
                                IStockRepository stockRepository,
                                IUpdateStockService updateStockService)
        {
            _stockTransactionsRepository = stockTransactionsRepository;
            _stockRepository = stockRepository;
            _updateStockService = updateStockService;
        }

        [HttpGet("StockRawData")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetAllStockTransactionsAsync()
        {
            var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();
            return Ok(stockInfo);
        }

        [HttpGet("StockAllInfo")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetAllStockStockBlanceProfitAsync(string name, string stockCode)
        {
            try
            {
                var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();
                var res = await _updateStockService.SortInfo(name, stockCode);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Inventory")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetAllStockInventorytionsAsync(string name, string stockCode)
        {
            try
            {
                var stockInfoActionResults = await GetAllStockStockBlanceProfitAsync(name, stockCode);

                if (stockInfoActionResults.Result is OkObjectResult)
                {
                    var stockInfo = (stockInfoActionResults.Result as OkObjectResult).Value as IEnumerable<StockTransactions>;
                    var sortedInfo = _updateStockService.SortStockInventoryAsync(stockInfo);
                    return Ok(sortedInfo);
                }
                else
                {
                    return stockInfoActionResults.Result;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("RealizedProfit")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>>
            GetRealizedProfitAsync(string name, string stockCode, DateTime startDate, DateTime endDate)
        {
            try
            {
                var stockInfoActionResults = await GetAllStockStockBlanceProfitAsync(name, stockCode);
                if (stockInfoActionResults.Result is OkObjectResult)
                {
                    var stockInfo = (stockInfoActionResults.Result as OkObjectResult).Value as IEnumerable<StockTransactions>;
                    var sortedInfo = _updateStockService.SortRealizedProfitAsync(stockInfo, startDate, endDate);
                    return Ok(sortedInfo);
                }
                else { return stockInfoActionResults.Result; }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("RealizedDividend")]
        public async Task<ActionResult<List<Dictionary<string, string>>>> GetRealizedDividend(DateTime startDate, DateTime endDate, string stockCode)
        {
            try
            {
                var res = _updateStockService.GetDividendAsync(startDate, endDate, stockCode);
                return await res;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}