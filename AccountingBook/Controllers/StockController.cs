using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services;
using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Transactions;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : Controller
    {
        private readonly StockTransactionsRepository _stockTransactionsRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUpdateStockService _updateStockService;

        public StockController(StockTransactionsRepository stockTransactionsRepository,
                                IStockRepository stockRepository,
                                IUpdateStockService updateStockService)
        {
            _stockTransactionsRepository = stockTransactionsRepository;
            _stockRepository = stockRepository;
            _updateStockService = updateStockService;
        }

        [HttpGet("StockRawData")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockTransactionsAsync()
        {
            try
            {
                var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();

                return Ok(stockInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("StockAllInfo")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockStockBlanceProfitAsync(string stockCode)
        {
            try
            {
                var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();
                var sortedStockInfo = stockInfo.OrderBy(s => s.StockName)
                                               .ThenBy(s => s.TransactionDate)
                                               .ThenBy(s => s.TransactionName)
                                               .Where(s => s.TransactionName == "吳瑞庭" && s.StockCode == stockCode)
                                               .ToList();
                for (int i = 0; i < sortedStockInfo.Count; i++)
                {
                    var balance = 0;
                    var profit = 0;
                    if (i == 0)
                    {
                        balance = sortedStockInfo[i].Deposit - sortedStockInfo[i].Withdrawal;
                        profit = 0;
                    }
                    else
                    {
                        var currentStock = sortedStockInfo[i];
                        var previousStock = sortedStockInfo[i - 1];

                        if (currentStock.StockName == previousStock.StockName)
                        {
                            balance = previousStock.Balance + currentStock.Deposit - currentStock.Withdrawal;
                        }
                        else
                        {
                            balance = sortedStockInfo[i].Deposit - sortedStockInfo[i].Withdrawal;
                        }
                    }

                    sortedStockInfo[i].Balance = balance;
                }

                foreach (var transaction in sortedStockInfo)
                {
                    if (transaction.Withdrawal != 0)
                    {
                        var purchasingInfo = sortedStockInfo.Where
                                                            (t => t.StockCode ==
                                                            transaction.StockCode
                                                            && t.Withdrawal == 0
                                                            && t.IsSell == false
                                                            && t.Profit == null).ToList();
                        var profit = 0;
                        var totalDeposit = 0;
                        var purchasingInfoCount = 0;
                        while (transaction.Withdrawal != totalDeposit && purchasingInfoCount < purchasingInfo.Count)
                        {
                            var purchasingPrice = purchasingInfo[purchasingInfoCount].PurchasingPrice;
                            var deposit = purchasingInfo[purchasingInfoCount].Deposit;
                            totalDeposit += deposit;
                            var income = (transaction.PurchasingPrice - purchasingPrice) * deposit;
                            var outcome = purchasingInfo[purchasingInfoCount].Fee;
                            profit += Convert.ToInt32(income - outcome);
                            purchasingInfo[purchasingInfoCount].IsSell = true;
                            transaction.IsSell = true;
                            purchasingInfoCount++;
                        }
                        profit -= Convert.ToInt32(transaction.Fee + transaction.Tax);
                        transaction.Profit = profit;
                    }
                }
                return Ok(sortedStockInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Inventory")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockInventorytionsAsync()
        {
            try
            {
                var stockInfoActionResults = await GetAllStockStockBlanceProfitAsync("006208");

                if (stockInfoActionResults.Result is OkObjectResult)
                {
                    var stockInfo = (stockInfoActionResults.Result as OkObjectResult).Value as IEnumerable<StockTransactions>;
                    var sortedInfo = stockInfo.Where(c => c.IsSell == false)
                                              .GroupBy(s => s.StockName)
                                              .Select(g => new
                                              {
                                                  StockName = g.Key,
                                                  StockCode = g.First().StockCode,
                                                  TotalCost = (int)g.Sum(s => s.PurchasingPrice * s.Deposit + s.Fee),
                                                  Balance = g.Max(s => s.Balance)
                                              })
                                              .Select(s => new
                                              {
                                                  s.StockName,
                                                  s.StockCode,
                                                  s.Balance,
                                                  Price = s.TotalCost / s.Balance,
                                                  ClosingPrice = _updateStockService.UpdateColesingkPricesAsync(s.StockCode).Result,
                                                  s.TotalCost,
                                                  Profit = _updateStockService.UpdateColesingkPricesAsync(s.StockCode).Result * s.Balance - (int)(s.TotalCost * 1.0037),
                                              });
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
    }
}