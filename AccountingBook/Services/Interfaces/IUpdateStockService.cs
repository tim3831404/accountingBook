using AccountingBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingBook.Services.Interfaces
{
    public interface IUpdateStockService
    {
        Task<decimal> UpdateColesingkPricesAsync(string stockCode);

        Task<IEnumerable<StockTransactions>> GetStockTransactions(DateTime startDate, DateTime endDate, String userName);

        Task<List<StockTransactions>> CalculateBalanceProfit(string name, string stockCode);

        Task GetBalanceAndProfitAsync(List<StockTransactions> transactions);

        Task<IEnumerable<object>> SortStockInventoryAsync(string name, string stockCode);

        Task<IEnumerable<object>> SortRealizedProfitAsync(string name, string stockCode, DateTime startDate, DateTime endDate);

        Task<Dictionary<string, string>> GetDividendAsync(DateTime startDate, DateTime endDate, string stockCode);
    }
}