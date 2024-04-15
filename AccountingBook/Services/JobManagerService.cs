using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using Dapper;
using FluentScheduler;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public class JobManagerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPDFService _pdfService;
        private readonly IGmailService _mailService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<JobManagerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly StockTransactionsRepository _stockTransactionsRepository;

        public JobManagerService(IServiceProvider serviceProvider,
                                 IPDFService pdfService,
                                 IGmailService mailService,
                                 UserRepository userRepository,
                                 ILogger<JobManagerService> logger,
                                 IHttpClientFactory httpClientFactory,
                                 IConfiguration configuration,
                                 StockTransactionsRepository stockTransactionsRepository)
        {
            _serviceProvider = serviceProvider;
            _pdfService = pdfService;
            _mailService = mailService;
            _userRepository = userRepository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _stockTransactionsRepository = stockTransactionsRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //UpdateStockTransactions();
            //UpdateStockPricesAsync();
            //JobManager.AddJob(() => UpdateStockTransactions(), s => s.ToRunEvery(1).Days().At(13, 30));
            //JobManager.AddJob(() => UpdateStockPricesAsync(), s => s.ToRunEvery(1).Days().At(13, 30));
            _logger.LogError($"Start JobManagerService ExecuteAsync");
            while (!stoppingToken.IsCancellationRequested)
            {
                // 在這裡可以處理其他背景任務的邏輯
                await Task.Delay(1000, stoppingToken);
            }
            JobManager.RemoveAllJobs();
        }

        private async Task UpdateStockTransactions()
        {
            var emailList = await _userRepository.GetAllEmailAsync();
            foreach (var email in emailList)
            {
                try
                {
                    var userEmail = email.Email;
                    var userName = await _userRepository.GetUserNamedByUserEmailAsync(userEmail);
                    var filePath = "GmailSource";
                    var numLettrs = 20;
                    var messages = await _mailService.GetMessages(userEmail, numLettrs);

                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            var body = await _mailService.GetMessageBody(userEmail, message.Id);

                            var attachments = await _mailService.GetPdfAttachmentsAsync(userEmail, message.Id);

                            if (attachments.Count > 0)
                            {
                                var pdfResult = await _pdfService.ExtractTextFromPdfAsync(filePath, userName, attachments[0]);
                                if (await _pdfService.SaveTransactionToDatabase(pdfResult))
                                {
                                    _mailService.SendEmail(userEmail, pdfResult);
                                }
                            }
                            else
                            {
                                // Log 沒有 PDF 的訊息
                                _logger.LogWarning($"No PDF attachments found in email with ID: {message.Id} for user: {email.Email}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No messages found for user: {email.Email}");
                    }
                }
                catch (Exception ex)
                {
                    // Log 錯誤訊息
                    _logger.LogError($"Error processing emails for user: {email.Email}. Error: {ex.Message}");
                }
            }
        }

        public async Task<string> UpdateStockPricesAsync()
        {
            var updateMessage = "";
            _logger.LogInformation("UpdateStockPriceAtSpecificTime is running.");
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

                    var allStocks = await stockRepository.GetAllStocksAsync();

                    foreach (var stock in allStocks)
                    {
                        // 替換成實際的第三方 API 網址
                        string tseCode = "tse_" + stock.StockCode + ".tw";
                        string otcCode = "otc_" + stock.StockCode + ".tw";
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
                                    var newPrice = stockInfoResponse.msgArray[0].pz;

                                    // 更新股票價格
                                    stock.ClosingPrice = newPrice;

                                    // 使用 Dapper 更新資料庫中的 ClosingPrice
                                    string connectionString = _configuration.GetConnectionString("StockDatabase");

                                    using (var connection = new SqlConnection(connectionString))
                                    {
                                        connection.Open();

                                        var affectedRows = await connection.ExecuteAsync(
                                            "UPDATE Stocks SET ClosingPrice = @ClosingPrice WHERE StockId = @StockId",
                                            new { ClosingPrice = newPrice, StockId = stock.StockId });

                                        if (affectedRows > 0)
                                        {
                                            updateMessage = "ClosingPrice 更新成功"; ;
                                        }
                                        else
                                        {
                                            updateMessage = "ClosingPrice 更新失敗";
                                        }
                                    }
                                }
                                else
                                {
                                    // 處理 API 請求失敗的情況
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateMessage = "發生錯誤：" + ex.Message;
            }

            return (updateMessage);
        }

        public async Task UpdateProfit()
        {
            var transactions = await _stockTransactionsRepository.GetInfoByProfitAsync();
            foreach (var transaction in transactions)
            {
                if (transaction.Withdrawal != 0)
                {
                    var purchasingInfo = transactions.Where
                                                        (t => t.StockCode ==
                                                        transaction.StockCode
                                                        && t.Balance != 0
                                                        && t.Withdrawal == 0
                                                        && t.Profit == null).ToList();
                    var profit = 0;
                    var totalDeposit = 0;
                    var purchasingInfoCount = 0;
                    while (transaction.Withdrawal != totalDeposit)
                    {
                        var purchasingPrice = purchasingInfo[purchasingInfoCount].PurchasingPrice;
                        var deposit = purchasingInfo[purchasingInfoCount].Deposit;
                        totalDeposit += deposit;
                        var income = (transaction.PurchasingPrice - purchasingPrice) * deposit;
                        var outcome = purchasingInfo[purchasingInfoCount].Fee;
                        profit += Convert.ToInt32(income - outcome);
                        await _stockTransactionsRepository.UpdateIncomeProfitAsync(purchasingInfo[purchasingInfoCount].TransactionId);
                        purchasingInfo[purchasingInfoCount].Profit = 0;
                        purchasingInfoCount++;
                    }
                    profit -= Convert.ToInt32(transaction.Fee + transaction.Tax);
                    transaction.Profit = profit;
                    await _stockTransactionsRepository.UpdateOutcomeProfitAsync(transaction.TransactionId, profit);
                }
            }
        }
    }
}