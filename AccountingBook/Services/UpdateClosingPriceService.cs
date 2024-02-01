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

namespace AccountingBook.Services
{
    public class UpdateClosingPriceService : BackgroundService
    {
        private Timer _timer;
        public IStockRepository _stockRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public UpdateClosingPriceService(
                            IStockRepository stockRepository,
                            IHttpClientFactory httpClientFactory
                            )
        {
            _stockRepository = stockRepository;
            _httpClientFactory = httpClientFactory;
        }




        public override Task StartAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(UpdateStockPriceAtSpecificTime, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Adjust the interval as needed
            return base.StartAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(stoppingToken);
        }

        private async void UpdateStockPriceAtSpecificTime(object state)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            // Run the update only at 13:30 PM
            if (currentTime.Hours == 13 && currentTime.Minutes == 30)
            {
                // Call your update method here
                await UpdateStockPricesAsync();
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Implement your background service logic here
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform background task
                await Task.Delay(1000, stoppingToken); // Adjust the delay as needed
            }
        }



        public async Task UpdateStockPricesAsync()
        {
            try
            {
                // 取得所有股票
                var allStocks = await _stockRepository.GetAllStocksAsync();

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
                                using (var connection = new SqlConnection("StockDatabase"))
                                {
                                    connection.Open();

                                    var affectedRows = await connection.ExecuteAsync(
                                        "UPDATE Stocks SET ClosingPrice = @ClosingPrice WHERE StockId = @StockId",
                                        new { ClosingPrice = newPrice, StockId = stock.StockId });

                                    if (affectedRows > 0)
                                    {
                                        // 更新成功，你可以進行相關的操作
                                    }
                                    else
                                    {
                                        // 更新失敗，處理失敗的情況
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
            catch (Exception ex)
            {
                // 處理例外狀況
            }
        }

    }
}
