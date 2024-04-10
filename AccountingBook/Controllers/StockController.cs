using AccountingBook.Interfaces;
using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Services;
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

        public StockController(StockTransactionsRepository stockTransactionsRepository,
                                IStockRepository stockRepository)
        {
            _stockTransactionsRepository = stockTransactionsRepository;
            _stockRepository = stockRepository;
        }

        [HttpGet("StockTransactions")]
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

        [HttpGet("StockBlance")]
        public async Task<ActionResult<IEnumerable<StockTransactions>>> GetAllStockStockBlanceAsync()
        {
            try
            {
                var stockInfo = await _stockTransactionsRepository.GetAllStockTransactionsAsync();
                var sortedStockInfo = stockInfo.OrderBy(s => s.TransactionDate)
                                               .ThenBy(s => s.TransactionName)
                                               .ThenBy(s => s.StockName)
                                               .Where(s => s.TransactionName == "吳瑞庭" && s.StockCode == "3372")
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
                    sortedStockInfo[i].Profit = profit;
                }

                for (int i = 0; i < sortedStockInfo.Count; i++)
                {
                    if (sortedStockInfo[i].Withdrawal != 0)
                    {
                        var sortedCodeStockInfo = sortedStockInfo
                                                  .Where(c => c.StockCode == sortedStockInfo[i].StockCode
                                                   && c.Profit == null)
                                                  .FirstOrDefault();
                    }
                }
                return Ok(sortedStockInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}