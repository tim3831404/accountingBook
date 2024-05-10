using AccountingBook.Models;
using AccountingBook.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public class UpdateStockService : IUpdateStockService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public UpdateStockService(
                            IServiceProvider serviceProvider,
                            IHttpClientFactory httpClientFactory,
                            IConfiguration configuration
                            )
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<decimal> UpdateColesingkPricesAsync(string stockCode)
        {
            string updateMessage = "";
            decimal closingPrice = 0;
            try
            {
                {
                    string tseCode = "tse_" + stockCode + ".tw";
                    string otcCode = "otc_" + stockCode + ".tw";
                    string urlTse = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={tseCode}";
                    string urlOtc = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={otcCode}";

                    using (WebClient wClient = new WebClient())
                    {
                        wClient.Encoding = Encoding.UTF8;
                        string downloadedTseData = wClient.DownloadString(urlTse);
                        StockInfoResponse stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedTseData);
                        String apiUrl = "";
                        if (stockInfoResponse.msgArray != null && stockInfoResponse.msgArray.Any())
                        {
                            apiUrl = urlTse;
                        }
                        else
                        {
                            string downloadedOtcData = wClient.DownloadString(urlOtc);
                            stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedOtcData);
                            apiUrl = urlOtc;
                        }

                        using (var httpClient = _httpClientFactory.CreateClient())
                        {
                            var response = await httpClient.GetAsync(apiUrl);

                            if (response.IsSuccessStatusCode)
                            {
                                closingPrice = stockInfoResponse.msgArray[0].pz;
                            }
                            else
                            {
                                Console.WriteLine("ClosingPrice not found");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateMessage = "發生錯誤：" + ex.Message;
            }

            return (closingPrice);
        }

        public async Task<IEnumerable<IGrouping<string, StockTransactions>>>
            SortInfo(IEnumerable<StockTransactions> stockInfo, string name, string stockCode)
        {
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
            return groupStockInfo;
        }

        public async Task GetBalanceAndProfitAsync(List<StockTransactions> transactions)
        {
            int balance = 0;
            int profit = 0;

            for (int i = 0; i < transactions.Count; i++)
            {
                if (i == 0)
                {
                    balance = transactions[i].Deposit - transactions[i].Withdrawal;
                    profit = 0;
                }
                else
                {
                    var currentStock = transactions[i];
                    var previousStock = transactions[i - 1];

                    balance = currentStock.StockCode == previousStock.StockCode ?
                              previousStock.Balance + currentStock.Deposit - currentStock.Withdrawal :
                              transactions[i].Deposit - transactions[i].Withdrawal;
                }

                transactions[i].Balance = balance;

                await GeteProfitAsync(transactions[i], transactions);
            }
        }

        private async Task GeteProfitAsync(StockTransactions transaction, List<StockTransactions> transactions)
        {
            if (transaction.Withdrawal != 0)
            {
                var purchasingInfo = transactions.Where
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

        public async Task<IEnumerable<object>> SortStockInventoryAsync(IEnumerable<StockTransactions> stockInfo)
        {
            try
            {
                var sortedInfo = stockInfo.Where(c => c.IsSell == false)
                                              .GroupBy(s => new { s.StockCode, s.TransactionName })
                                              .Select(g => new
                                              {
                                                  StockCode = g.Key,
                                                  StockName = g.First().StockName,
                                                  TransactionName = g.First().TransactionName,
                                                  Cost = (int)g.Sum(s => s.PurchasingPrice * s.Deposit),
                                                  Fee = (int)g.Sum(s => s.Fee),
                                                  Balance = (int)stockInfo.Where(c => c.StockCode == g.Key.StockCode &&
                                                                                      c.TransactionName == g.Key.TransactionName)
                                                            .OrderByDescending(c => c.TransactionDate)
                                                            .FirstOrDefault().Balance,
                                              })
                                              .Select(s => new
                                              {
                                                  s.TransactionName,
                                                  s.StockName,
                                                  s.StockCode.StockCode,
                                                  s.Balance,
                                                  Price = ((double)s.Cost / s.Balance).ToString("N2"),
                                                  ClosingPrice = UpdateColesingkPricesAsync(s.StockCode.StockCode)
                                                                .Result,
                                                  TotalCost = s.Cost + s.Fee,
                                                  Profit = UpdateColesingkPricesAsync(s.StockCode.StockCode).Result *
                                                           s.Balance - (int)((s.Cost + s.Fee) * 1.001425 + s.Fee),
                                              })
                                              .ToList();
                var totalProfit = sortedInfo.Sum(s => s.Profit);
                var totalCost = sortedInfo.Sum(s => s.TotalCost);
                sortedInfo.Add(new
                {
                    TransactionName = "Total",
                    StockName = "Total",
                    StockCode = "Total",
                    Balance = 0,
                    Price = "null",
                    ClosingPrice = 0m,
                    TotalCost = totalCost,
                    Profit = totalProfit
                });

                return sortedInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in CalculateStockInventoryAsync: {ex.Message}");
            }
        }
    }
}