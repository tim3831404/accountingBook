using AccountingBook.Interfaces;
using AccountingBook.Repository;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using System.Text;
using AccountingBook.Models;
using Newtonsoft.Json;
using System.Linq;

namespace AccountingBook.Services
{
    public class StockService
    {
        //readonly private IStockRepository _stockRepository;
        //public StockService(IStockRepository stockRepository)
        //{
        //    _stockRepository = stockRepository;
        //}

        //public Dictionary<int, string> GetTodayClosingPrices()
        //{
        //    Dictionary<int, string> ClosingPrices = new Dictionary<int, string>();
        //    try
        //    {
        //        IEnumerable<int> StockIds = (IEnumerable<int>)_stockRepository.GetAllStocksIdAsync();

        //        foreach (int StockId in StockIds)
        //        {
        //            // 调用获取实时股价的方法
        //            string closingPrice = GetClosingPriceForStock(StockId);

        //            // 将结果添加到字典中
        //            ClosingPrices.Add(StockId, closingPrice);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // 处理异常，可以记录日志或者进行其他处理
        //        Console.WriteLine($"發生異常: {ex.Message}");
        //    }

        //    return ClosingPrices;
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

                string jsonDataSuccess = "Your JSON string for success response";
                string jsonDataError = "Your JSON string for error response";
                StockInfoResponse stockInfoResponse = JsonConvert.DeserializeObject<StockInfoResponse>(jsonDataSuccess);
                if (stockInfoResponse.msgArray != null && stockInfoResponse.msgArray.Any())
                {
                    return downloadedTseData;
                }

         
                
                else
                {
                    string downloadedOtcData = wClient.DownloadString(urlOtc);
                    return downloadedOtcData;
                }
            }

            return "這支股票不屬於上市或者上櫃";
        }

    }
}
