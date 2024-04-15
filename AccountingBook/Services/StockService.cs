using AccountingBook.Models;
using AccountingBook.Repository.Interfaces;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Text;

namespace AccountingBook.Services
{
    public class StockService
    {
        private readonly IStockRepository _stockRepository;

        public StockService(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        //public void UpdateTodayClosingPrices()
        //{
        //try
        //{
        //    IEnumerable<int> stockIds = _stockRepository.GetAllStocksAsync();

        //    foreach (int stockId in stockIds)
        //    {
        //        // 调用获取实时股价的方法
        //        decimal closingPrice = GetClosingPriceForStock(stockId);

        //        // 更新股票的今日收盘价
        //        _stockRepository.UpdateStockClosingPrice(stockId, closingPrice);
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // 处理异常，可以记录日志或者进行其他处理
        //    Console.WriteLine($"發生異常: {ex.Message}");
        //}
        //}

        public string GetClosingPriceForStock(int StockId)
        {
            string tseCode = "tse_" + StockId + ".tw";
            string otcCode = "otc_" + StockId + ".tw";
            string urlTse = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={tseCode}";
            string urlOtc = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch={otcCode}";

            using (WebClient wClient = new WebClient())
            {
                wClient.Encoding = Encoding.UTF8;

                string downloadedTseData = wClient.DownloadString(urlTse);

                StockInfoResponse stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedTseData);
                if (stockInfoResponse.msgArray != null && stockInfoResponse.msgArray.Any())
                {
                    foreach (var stockInfoItem in stockInfoResponse.msgArray)
                    {
                        return $"股票代碼: {stockInfoItem.c}, 收盤價: {stockInfoItem.pz}";
                    }
                }
                else
                {
                    string downloadedOtcData = wClient.DownloadString(urlOtc);
                    stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(downloadedOtcData);

                    foreach (var stockInfoItem in stockInfoResponse.msgArray)
                    {
                        return $"股票代碼: {stockInfoItem.c}, 收盤價: {stockInfoItem.pz}";
                    }
                }

                return "1.不屬於上市或上櫃 2.代碼輸入錯誤";
            }
        }
    }
}