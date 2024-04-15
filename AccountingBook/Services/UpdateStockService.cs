using AccountingBook.Models;
using AccountingBook.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
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
    }
}