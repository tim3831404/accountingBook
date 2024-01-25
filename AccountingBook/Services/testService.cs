//using AccountingBook.Models;
//using Newtonsoft.Json;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System;
//using System.Linq;
//using System.Net;
//using System.Text;
//using AccountingBook.Interfaces;

//namespace AccountingBook.Services
//{
//    public class testService
//    {
//        readonly private IStockRepository _stockRepository;
//        public testService(IStockRepository stockRepository)
//        {
//            _stockRepository = stockRepository;
//        }
//        private readonly HttpClient _httpClient;

//        public testService(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }

//        public async Task UpdateStockPriceAsync(Stocks stock)
//        {
//            try
//            {
//                // 替換成實際的第三方 API 網址
//                string tseCode = "tse_" + stock.StockCode + ".tw";
//                string otcCode = "otc_" + stock.StockCode + ".tw";
//                string urlTse = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={tseCode}";
//                string urlOtc = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={otcCode}";


//                using (WebClient wClient = new WebClient())
//                {
//                    wClient.Encoding = Encoding.UTF8;
//                    string downloadedTseData = wClient.DownloadString(urlTse);
//                    StockInfoResponse stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedTseData);
//                    String apiUrl = "";
//                    if (stockInfoResponse.msgArray != null && stockInfoResponse.msgArray.Any())
//                    {
//                        apiUrl = urlTse;
//                    }
//                    else
//                    {
//                        string downloadedOtcData = wClient.DownloadString(urlOtc);
//                        stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedOtcData);
//                        apiUrl = urlOtc;
//                    }

//                    var response = await _httpClient.GetAsync(apiUrl);
                  

//                    if (response.IsSuccessStatusCode)
//                    {
//                        var newPrice = stockInfoResponse.msgArray[0].pz;

//                        // 更新股票價格
//                        stock.ClosingPrice = newPrice;
//                        // 這裡可以進行其他相關的更新操作，例如存入資料庫
//                    }
//                    else
//                    {
//                        // 處理 API 請求失敗的情況
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                // 處理例外狀況
//            }
//        }
//    }
//}
