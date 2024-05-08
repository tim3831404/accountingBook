using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    public class DividendController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public DividendController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<IActionResult> GetDividendData(DateTime startDate, DateTime endDate, string stockCode)
        {
            var url = "https://api.finmindtrade.com/api/v4/data";

            var parameters = new Dictionary<string, string>
        {
            { "dataset", "TaiwanStockDividend" },
            { "start_date", startDate.ToString("yyyy-MM-dd") },
            { "end_date", endDate.ToString("yyyy-MM-dd") },
            { "data_id", stockCode }
        };

            var response = await _httpClient.GetAsync(url + ToQueryString(parameters));

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            var div = new List<Dictionary<string, string>>();
            foreach (var item in data["data"])
            {
                var dict = new Dictionary<string, string>
            {
                {"date", item["CashExDividendTradingDate"].ToString()},
                {"Dividends", (item["CashEarningsDistribution"]+item["CashStatutorySurplus"]).ToString()},
                {"StockEarnings", (item["StockEarningsDistribution"]*2).ToString()}
            };
                if (!dict.ContainsValue(""))
                {
                    div.Add(dict);
                }
            }

            return Ok(div);
        }

        private string ToQueryString(Dictionary<string, string> parameters)
        {
            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return "?" + queryString;
        }
    }
}