using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services.Interfaces;
using Dapper;
using Google.Apis.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace AccountingBook.Services
{
    public class UpdateStockService : IUpdateStockService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly IStockTransactionsRepository _stockTransactionsRepository;
        private readonly IDividendsRepository _iDividendsRepository;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(100, 100);
        private static DateTime _resetTime = DateTime.UtcNow.AddHours(1);

        public UpdateStockService(
                            IServiceProvider serviceProvider,
                            IHttpClientFactory httpClientFactory,
                            IStockTransactionsRepository stockTransactionsRepository,
                            IDividendsRepository iDividendsRepository,
                            IConfiguration configuration
                            )
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _stockTransactionsRepository = stockTransactionsRepository;
            _iDividendsRepository = iDividendsRepository;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<StockTransactions>> GetStockTransactions(DateTime startDate, DateTime endDate, String userName)

        {
            var stockInfo = await _stockTransactionsRepository.GetInfoByDateAndUserAsync(startDate, endDate, userName);
            return stockInfo;
        }

        public async Task<decimal> UpdateColesingkPricesAsync(string stockCode)
        {
            string updateMessage = "";
            decimal closingPrice = 0;
            try
            {
                {
                    var tseCode = "tse_" + stockCode + ".tw";
                    var otcCode = "otc_" + stockCode + ".tw";
                    var apiUrl = _configuration["StockSource:Url"];
                    var urlTse = apiUrl + tseCode;
                    var urlOtc = apiUrl + otcCode;
                    using (WebClient wClient = new WebClient())
                    {
                        wClient.Encoding = Encoding.UTF8;
                        string downloadedTseData = wClient.DownloadString(urlTse);
                        StockInfoResponse stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedTseData);
                        var apiSource = "";
                        if (stockInfoResponse.msgArray != null && stockInfoResponse.msgArray.Any())
                        {
                            apiSource = urlTse;
                        }
                        else
                        {
                            string downloadedOtcData = wClient.DownloadString(urlOtc);
                            stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedOtcData);
                            apiSource = urlOtc;
                        }

                        using (var httpClient = _httpClientFactory.CreateClient())
                        {
                            var response = await httpClient.GetAsync(apiSource);

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

        public async Task<List<StockTransactions>> CalculateBalanceProfit(string name, string stockCode)
        {
            var stockInfo = await _stockTransactionsRepository.GetInfoByStockCodeNameAsync(name, stockCode);
            var groupStockInfo = stockInfo
                                   .OrderBy(s => s.StockCode)
                                   .ThenBy(s => s.TransactionDate)
                                   .GroupBy(s => s.TransactionName);
            var res = new List<StockTransactions>();
            foreach (var group in groupStockInfo)
            {
                var sortedStockInfo = group.ToList();
                await GetBalanceAndProfitAsync(sortedStockInfo);
                res.AddRange(sortedStockInfo);
            }
            return res;
        }

        private async Task GetBalanceAndProfitAsync(List<StockTransactions> transactions)
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

        public async void SaveDividendToDatabase(List<Dividends> dividends)
        {
            string connectionString = _configuration.GetConnectionString("StockDatabase");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var dividend in dividends)
                {
                    var existingDividend = connection.QueryFirstOrDefault<Dividends>(
                    @"SELECT * FROM Dividends
                    WHERE TransactionDate = @TransactionDate
                    AND StockCode = @StockCode
                    AND StockName = @StockName
                    AND TransactionName = @TransactionName
                    AND COALESCE(PaymentDate, '1900-01-01') = COALESCE(@PaymentDate, '1900-01-01')
                    AND TransactionId = @TransactionId",
                    dividend);

                    if (existingDividend == null)
                    {
                        connection.Execute(@"INSERT INTO Dividends
                            (TransactionDate, TransactionName, StockCode, StockName, PaymentDate, DividendTradingDate, AmountCash, AmountStock, TransactionId)
                            VALUES
                            (@TransactionDate, @TransactionName, @StockCode, @StockName, @PaymentDate, @DividendTradingDate, @AmountCash, @AmountStock, @TransactionId)",
                                dividend);
                    }
                }
            }
        }

        public async Task<List<Dictionary<string, string>>> GetDividendAsync(DateTime startDate, DateTime endDate, string stockCode, int transationId)
        {
            var dividenasdIdSet = await _iDividendsRepository.GetTransationIdInDividends();
            var IsInInventory = await IsInInventoryAsync(null, stockCode);
            var res = new List<Dictionary<string, string>>();
            if (!dividenasdIdSet.Contains(transationId) || IsInInventory)
            {
                await EnsureRateLimit();
                var apiUrl = _configuration["DividendSource:Url"];
                var parameters = new Dictionary<string, string>
        {
            { "dataset", "TaiwanStockDividend" },
            { "start_date", startDate.ToString("yyyy-MM-dd") },
            { "end_date", endDate.ToString("yyyy-MM-dd") },
            { "data_id", stockCode }
        };

                var response = await _httpClient.GetAsync(apiUrl + ToQueryString(parameters));

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);

                res = new List<Dictionary<string, string>>();
                foreach (var item in data["data"])
                {
                    var dict = new Dictionary<string, string>
            {
                {"CashExDividendTradingDate", item["CashExDividendTradingDate"].ToString()},
                {"CashDividendPaymentDate", (item["CashDividendPaymentDate"]).ToString()},
                {"Dividends", (item["CashEarningsDistribution"]+item["CashStatutorySurplus"]).ToString()},
                {"StockEarnings", (item["StockEarningsDistribution"]*2).ToString()}
            };
                    if (!dict.ContainsValue(""))
                    {
                        res.Add(dict);
                    }
                }
            }

            return res;
        }

        public async Task<List<Dividends>> CaculateDividendAsync(string name, string stockCode)
        {
            var nowDate = DateTime.Now.Date;
            var res = new List<Dividends>();
            var stockInfos = await CalculateBalanceProfit(name, stockCode);
            var skipId = new List<int>();
            StockTransactions shellInfo = null;
            foreach (var stockInfo in stockInfos)
            {
                if (skipId.Count == 0)
                {
                    shellInfo = stockInfos.Where(x => x.Withdrawal != 0)
                                          .OrderBy(x => x.TransactionDate)
                                          .FirstOrDefault();
                }
                else
                {
                    shellInfo = stockInfos.Where(x => x.Withdrawal != 0 && !skipId.Contains(x.TransactionId))
                                          .OrderBy(x => x.TransactionDate)
                                          .FirstOrDefault();
                }

                var transactionDate = stockInfo.TransactionDate;
                if (stockInfo.IsSell && stockInfo.Deposit > 0)
                {
                    var shellDate = shellInfo.TransactionDate;
                    if (stockInfo.Balance == shellInfo.Withdrawal)
                    {
                        skipId.Add(shellInfo.TransactionId);
                    }

                    res.AddRange(await creatDividend(transactionDate, shellDate, stockInfo.StockCode, stockInfo, shellInfo));
                }
                else if (stockInfo.Deposit > 0)
                {
                    res.AddRange(await creatDividend(transactionDate, nowDate, stockInfo.StockCode, stockInfo, shellInfo));
                }
            }
            if (res.Count > 0)
            {
                SaveDividendToDatabase(res);
            }

            return res;
        }

        public async Task<IEnumerable<object>> SumStockInventoryAsync(string name, string stockCode)
        {
            var stockInfo = await CalculateBalanceProfit(name, stockCode);
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
            //var sortedInfo = stockInfo.Where(c => c.IsSell == false)
            //                              .GroupBy(s => new { s.StockCode, s.TransactionName })
            //                              .Select(g => new
            //                              {
            //                                  StockCode = g.Key,
            //                                  StockName = g.First().StockName,
            //                                  TransactionName = g.First().TransactionName,
            //                                  Cost = (int)g.Sum(s => s.PurchasingPrice * s.Deposit),
            //                                  Fee = (int)g.Sum(s => s.Fee),
            //                                  Balance = (int)stockInfo.Where(c => c.StockCode == g.Key.StockCode &&
            //                                                                      c.TransactionName == g.Key.TransactionName)
            //                                            .OrderByDescending(c => c.TransactionDate)
            //                                            .FirstOrDefault().Balance,
            //                              })
            //                              .Select(s => new
            //                              {
            //                                  s.TransactionName,
            //                                  s.StockName,
            //                                  s.StockCode.StockCode,
            //                                  s.Balance,
            //                                  Price = ((double)s.Cost / s.Balance).ToString("N2"),
            //                                  ClosingPrice = UpdateColesingkPricesAsync(s.StockCode.StockCode)
            //                                                .Result,
            //                                  TotalCost = s.Cost + s.Fee,
            //                                  Profit = UpdateColesingkPricesAsync(s.StockCode.StockCode).Result *
            //                                           s.Balance - (int)((s.Cost + s.Fee) * 1.001425 + s.Fee),
            //                              })
            //                              .ToList();
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

        public async Task<IEnumerable<object>> SortRealizedProfitAsync(string name, string stockCode
                                                                       , DateTime startDate, DateTime endDate)
        {
            try
            {
                var stockInfo = await CalculateBalanceProfit(name, stockCode);
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
                    TotalCost = totalCost,
                    Profit = (int?)totalProfit
                });
                return sortedInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in CalculateStockInventoryAsync: {ex.Message}");
            }
        }

        public async Task<IEnumerable<object>> SortDividendAsync(DateTime startDate, DateTime endDate)
        {
            var dividendsInfo = await _iDividendsRepository.FilterDividendsByPaymentDate(startDate, endDate);
            var sortedDividendsInfo = dividendsInfo.GroupBy(s => new { s.StockCode, s.TransactionName })
                                          .Select(g => new
                                          {
                                              GroupInfo = g.Key,
                                              StockName = g.First().StockName,
                                              TransactionName = g.First().TransactionName,
                                              AmountCash = (int)g.Sum(s => s.AmountCash),
                                              AmountStock = (int)g.Sum(s => s.AmountStock),
                                          })
                                          .Select(s => new
                                          {
                                              TransactionName = s.GroupInfo.TransactionName,
                                              StockName = s.StockName,
                                              StockCode = s.GroupInfo.StockCode,
                                              AmountCash = s.AmountCash,
                                              AmountStock = s.AmountStock,
                                          }
                                          ).ToList();
            var totalAmountCash = sortedDividendsInfo.Sum(s => s.AmountCash);
            var totalAmountStock = sortedDividendsInfo.Sum(s => s.AmountStock);
            sortedDividendsInfo.Add(new
            {
                TransactionName = "Total",
                StockName = "Total",
                StockCode = "Total",
                AmountCash = totalAmountCash,
                AmountStock = totalAmountStock,
            });

            return sortedDividendsInfo;
        }

        private string ToQueryString(Dictionary<string, string> parameters)
        {
            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return "?" + queryString;
        }

        private async Task EnsureRateLimit()
        {
            await _semaphore.WaitAsync();

            if (DateTime.UtcNow >= _resetTime)
            {
                lock (_semaphore)
                {
                    if (DateTime.UtcNow >= _resetTime)
                    {
                        _semaphore.Release(100 - _semaphore.CurrentCount);
                        _resetTime = DateTime.UtcNow.AddHours(1);
                    }
                }
            }
        }

        private async Task<List<Dividends>> creatDividend(DateTime startDate, DateTime endDate, string stockCode, StockTransactions stockInfo, StockTransactions shellInfo)
        {
            var res = new List<Dividends>();
            var sortDividend = await GetDividendAsync(startDate, endDate, stockCode, stockInfo.TransactionId);
            if (sortDividend.Count == 0)
            {
               
                var dividend = new Dividends();
                dividend.copy(stockInfo);
                res.Add(dividend);
            }
            if (shellInfo == null)
            {
                foreach (var dividendInfo in sortDividend)
                {
                    var dividendTradingDate = DateTime.Parse(dividendInfo["CashExDividendTradingDate"]).Date;
                    var dividend = new Dividends
                    {
                        TransactionDate = stockInfo.TransactionDate.Date,
                        TransactionName = stockInfo.TransactionName,
                        StockCode = stockInfo.StockCode,
                        StockName = stockInfo.StockName,
                        PaymentDate = null,
                        DividendTradingDate = null,
                        AmountCash = 0,
                        AmountStock = 0,
                        TransactionId = stockInfo.TransactionId,
                    };
                    if (dividendTradingDate == stockInfo.TransactionDate.Date)
                    {
                        res.Add(dividend);
                    }
                    else
                    {
                        dividend = new Dividends
                        {
                            TransactionDate = stockInfo.TransactionDate.Date,
                            TransactionName = stockInfo.TransactionName,
                            StockCode = stockInfo.StockCode,
                            StockName = stockInfo.StockName,
                            PaymentDate = DateTime.Parse(dividendInfo["CashDividendPaymentDate"]).Date,
                            DividendTradingDate = dividendTradingDate,
                            AmountCash = decimal.Parse(dividendInfo["Dividends"]) * stockInfo.Deposit,
                            AmountStock = decimal.Parse(dividendInfo["StockEarnings"]) * (stockInfo.Deposit / 10),
                            TransactionId = stockInfo.TransactionId,
                        };
                        res.Add(dividend);
                    }
                }
            }
            else if ((stockInfo.Deposit <= shellInfo.Withdrawal))
            {
                foreach (var dividendInfo in sortDividend)
                {
                    var dividendTradingDate = DateTime.Parse(dividendInfo["CashExDividendTradingDate"]).Date;
                    var dividend = new Dividends
                    {
                        TransactionDate = stockInfo.TransactionDate.Date,
                        TransactionName = stockInfo.TransactionName,
                        StockCode = stockInfo.StockCode,
                        StockName = stockInfo.StockName,
                        PaymentDate = null,
                        DividendTradingDate = null,
                        AmountCash = 0,
                        AmountStock = 0,
                        TransactionId = stockInfo.TransactionId,
                    };
                    if (dividendTradingDate == stockInfo.TransactionDate.Date)
                    {
                        res.Add(dividend);
                    }
                    else
                    {
                        dividend = new Dividends
                        {
                            TransactionDate = stockInfo.TransactionDate,
                            TransactionName = stockInfo.TransactionName,
                            StockCode = stockInfo.StockCode,
                            StockName = stockInfo.StockName,
                            PaymentDate = DateTime.Parse(dividendInfo["CashDividendPaymentDate"]),
                            DividendTradingDate = DateTime.Parse(dividendInfo["CashExDividendTradingDate"]),
                            AmountCash = decimal.Parse(dividendInfo["Dividends"]) * stockInfo.Deposit,
                            AmountStock = decimal.Parse(dividendInfo["StockEarnings"]) * (stockInfo.Deposit / 10),
                            TransactionId = stockInfo.TransactionId,
                        };
                        res.Add(dividend);
                    }
                }
            }
            else
            {
                foreach (var dividendInfo in sortDividend)
                {
                    var dividendTradingDate = DateTime.Parse(dividendInfo["CashExDividendTradingDate"]).Date;
                    var dividend = new Dividends
                    {
                        TransactionDate = stockInfo.TransactionDate.Date,
                        TransactionName = stockInfo.TransactionName,
                        StockCode = stockInfo.StockCode,
                        StockName = stockInfo.StockName,
                        PaymentDate = null,
                        DividendTradingDate = null,
                        AmountCash = 0,
                        AmountStock = 0,
                        TransactionId = stockInfo.TransactionId,
                    };
                    if (dividendTradingDate == stockInfo.TransactionDate.Date)
                    {
                        res.Add(dividend);
                    }
                    else
                    {
                        dividend = new Dividends
                        {
                            TransactionDate = stockInfo.TransactionDate,
                            TransactionName = stockInfo.TransactionName,
                            StockCode = stockInfo.StockCode,
                            StockName = stockInfo.StockName,
                            PaymentDate = DateTime.Parse(dividendInfo["CashDividendPaymentDate"]),
                            DividendTradingDate = DateTime.Parse(dividendInfo["CashExDividendTradingDate"]),
                            AmountCash = decimal.Parse(dividendInfo["Dividends"]) * (stockInfo.Deposit - shellInfo.Withdrawal),
                            AmountStock = decimal.Parse(dividendInfo["StockEarnings"]) * ((stockInfo.Deposit - shellInfo.Withdrawal) / 10),
                            TransactionId = stockInfo.TransactionId,
                        };
                        res.Add(dividend);
                    }
                }
            }
            return res;
        }

        private async Task<bool> IsInInventoryAsync(string name, string stockCode)
        {
            var stockInfo = await CalculateBalanceProfit(name, stockCode);
            var sortedInfo = stockInfo.Where(c => c.IsSell == false);
            return sortedInfo.Count() == 0 ? false : true;
        }
    }
}