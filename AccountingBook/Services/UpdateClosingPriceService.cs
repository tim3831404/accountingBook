using AccountingBook.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Net;
using System.Text;
using AccountingBook.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AccountingBook.Services
{
    public class UpdateClosingPriceService : BackgroundService
    {
        private readonly ILogger<UpdateClosingPriceService> _logger;
        private readonly IConfiguration _configuration;
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        public UpdateClosingPriceService(
                            IServiceProvider serviceProvider,
                            IHttpClientFactory httpClientFactory,
                            ILogger<UpdateClosingPriceService> logger,
                            IConfiguration configuration
                            )
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }
        public override Task StartAsync(CancellationToken stoppingToken)
        {
            var now = DateTime.Now;
            var nextRunTime = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0);
            if (now > nextRunTime)
            {
                nextRunTime = nextRunTime.AddDays(1);
            }

            var delay = nextRunTime - now;

            _timer = new Timer(UpdateStockPriceAtSpecificTime, null, delay, TimeSpan.FromHours(1));

            return base.StartAsync(stoppingToken);
        }
        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(stoppingToken);
        }
        private async void UpdateStockPriceAtSpecificTime(object state)
        {
            if (true)
            {
                await UpdateStockPricesAsync(CancellationToken.None);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
            }
        }
        public async Task<string> UpdateStockPricesAsync(CancellationToken stoppingToken)
        {
            string updateMessage = "";
            _logger.LogInformation("UpdateStockPriceAtSpecificTime is running.");
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

                    // 取得所有股票
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
    }
}