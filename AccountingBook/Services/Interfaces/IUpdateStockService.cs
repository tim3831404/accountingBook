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

        Task<List<StockTransactions>> SortInfo(string name, string stockCode);

        Task GetBalanceAndProfitAsync(List<StockTransactions> transactions);

        Task<IEnumerable<object>> SortStockInventoryAsync(IEnumerable<StockTransactions> stockInfo);

        Task<IEnumerable<object>> SortRealizedProfitAsync(IEnumerable<StockTransactions> stockInfo, DateTime startDate, DateTime endDate);

        Task<List<Dictionary<string, string>>> GetDividendAsync(DateTime startDate, DateTime endDate, string stockCode);
    }
}