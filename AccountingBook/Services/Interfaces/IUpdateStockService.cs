using AccountingBook.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingBook.Services.Interfaces
{
    public interface IUpdateStockService
    {
        Task<decimal> UpdateColesingkPricesAsync(string stockCode);

        Task<IEnumerable<IGrouping<string, StockTransactions>>>
            SortInfo(IEnumerable<StockTransactions> stockInfo, string name, string stockCode);

        Task GetBalanceAndProfitAsync(List<StockTransactions> transactions);

        Task<IEnumerable<object>> SortStockInventoryAsync(IEnumerable<StockTransactions> stockInfo);
    }
}