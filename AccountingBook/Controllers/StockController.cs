using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services;
using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Crypto.Generators;
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
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockStockBlanceProfitAsync(string name, string stockCode)
        {
            try
            {
                var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();

                if (name != null)
                {
                    stockInfo = stockInfo.Where(s => s.TransactionName == name).ToList();
                }

                if (stockCode != null)
                {
                    stockInfo = stockInfo.Where(s => s.StockCode == stockCode).ToList();
                }

                var groupStockInfo = stockInfo
                                       .OrderBy(s => s.StockCode)
                                       .ThenBy(s => s.TransactionDate)
                                       .GroupBy(s => s.TransactionName);

                var res = new List<StockTransactions>();
                foreach (var group in groupStockInfo)
                {
                    var sortedStockInfo = group.ToList();
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

                            if (currentStock.StockCode == previousStock.StockCode)
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

                            while (transaction.Withdrawal != totalDeposit && purchasingInfoCount < purchasingInfo.Count) //
                            {
                                var purchasingPrice = purchasingInfo[purchasingInfoCount].PurchasingPrice;
                                var deposit = purchasingInfo[purchasingInfoCount].Deposit; ;
                                totalDeposit += deposit;

                                decimal? income;
                                if (transaction.Withdrawal < deposit)
                                {
                                    income = (transaction.PurchasingPrice - purchasingPrice) * transaction.Withdrawal;
                                    deposit -= transaction.Withdrawal;
                                }
                                else
                                {
                                    income = (transaction.PurchasingPrice - purchasingPrice) * deposit;
                                }

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
                    res.AddRange(sortedStockInfo);
                }
                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("Inventory")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockInventorytionsAsync(string name, string stockCode)
        {
            try
            {
                var stockInfoActionResults = await GetAllStockStockBlanceProfitAsync(name, stockCode);

                if (stockInfoActionResults.Result is OkObjectResult)
                {
                    var stockInfo = (stockInfoActionResults.Result as OkObjectResult).Value as IEnumerable<StockTransactions>;
                    var sortedInfo = stockInfo.Where(c => c.IsSell == false)
                                              .GroupBy(s => s.StockCode)
                                              .Select(g => new
                                              {
                                                  StockCode = g.Key,
                                                  StockName = g.First().StockName,
                                                  Cost = (int)g.Sum(s => s.PurchasingPrice * s.Deposit),
                                                  Fee = (int)g.Sum(s => s.Fee),
                                                  Balance = (int)stockInfo.Where(c => c.StockCode == g.Key)
                                                            .OrderByDescending(c => c.TransactionDate)
                                                            .FirstOrDefault().Balance,
                                              })
                                              .Select(s => new
                                              {
                                                  s.StockName,
                                                  s.StockCode,
                                                  s.Balance,
                                                  Price = ((double)s.Cost / s.Balance).ToString("N2"),
                                                  ClosingPrice = _updateStockService
                                                                .UpdateColesingkPricesAsync(s.StockCode)
                                                                .Result,
                                                  TotalCost = s.Cost + s.Fee,
                                                  Profit = _updateStockService
                                                           .UpdateColesingkPricesAsync(s.StockCode).Result *
                                                           s.Balance - (int)((s.Cost + s.Fee) * 1.001425 + s.Fee),
                                              })
                                              .ToList();
                    var totalProfit = sortedInfo.Sum(s => s.Profit);
                    var totalCost = sortedInfo.Sum(s => s.TotalCost);
                    sortedInfo.Add(new
                    {
                        StockName = "Total",
                        StockCode = "Total",
                        Balance = 0,
                        Price = "null",
                        ClosingPrice = 0m,
                        TotalCost = totalCost,
                        Profit = totalProfit
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

        [HttpPost("RealizedProfit")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetRealizedProfitAsync(string name, string stockCode, DateTime startDate, DateTime endDate)
        {
            try
            {
                var stockInfoActionResults = await GetAllStockStockBlanceProfitAsync(name, stockCode);
                if (stockInfoActionResults.Result is OkObjectResult)
                {
                    var stockInfo = (stockInfoActionResults.Result as OkObjectResult).Value as IEnumerable<StockTransactions>;

                    var sortedInfo = stockInfo.Where(c =>
                                                c.IsSell == true &&
                                                c.Withdrawal == 0)
                                         .GroupBy(s => new { s.StockCode, s.TransactionName })
                                         .Select(g => new
                                         {
                                             TransactionName = g.Key.TransactionName,
                                             StockCode = g.Key.StockCode,
                                             StockName = g.First().StockName,
                                             Cost = (int)g.Sum(s => s.PurchasingPrice * s.Deposit),
                                             Fee = (int)g.Sum(s => s.Fee),
                                             Balance = (int)g.Max(s => s.Balance),
                                         })
                                          .Select(s => new
                                          {
                                              s.TransactionName,
                                              s.StockName,
                                              s.StockCode,
                                              s.Balance,
                                              buyPrice = ((double)s.Cost / s.Balance).ToString("N2"),
                                              TotalCost = s.Cost + s.Fee,
                                              Profit =
                                              stockInfo.Where(c => c.TransactionDate >= startDate &&
                                                c.TransactionDate <= endDate &&
                                                c.IsSell == true &&
                                                c.Withdrawal != 0 &&
                                                c.StockCode == s.StockCode &&
                                                c.TransactionName == s.TransactionName)
                                                 .Sum(a => a.Profit),
                                              //sellPrice =
                                              //  (decimal?)stockInfo.Where(c => c.TransactionDate >= startDate &&
                                              //  c.TransactionDate <= endDate &&
                                              //  c.IsSell == true &&
                                              //  c.Withdrawal != 0 &&
                                              //  c.StockCode == s.StockCode)
                                              //   .FirstOrDefault()?.PurchasingPrice ?? 0m
                                          })
                                          .Where(c => c.Profit != 0)
                                          .ToList();

                    var totalCost = sortedInfo.Sum(s => s.TotalCost);
                    var totalProfit = sortedInfo.Sum(s => s.Profit);
                    sortedInfo.Add(new
                    {
                        TransactionName = "Total",
                        StockName = "Total",
                        StockCode = "Total",
                        Balance = 0,
                        buyPrice = "null",
                        //sellPrice = (decimal?)0m,
                        TotalCost = totalCost,
                        Profit = (int?)totalProfit
                    });

                    return Ok(sortedInfo);
                }
                else { return stockInfoActionResults.Result; }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}