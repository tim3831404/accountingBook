using AccountingBook.Models;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services.Interfaces;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public class StockService
    {
        private readonly IStockRepository _stockRepository;

        public StockService(IStockRepository stockRepository
                            )
        {
            _stockRepository = stockRepository;
        }

        public async Task<string> GetClosingPriceForStock(int StockId)
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