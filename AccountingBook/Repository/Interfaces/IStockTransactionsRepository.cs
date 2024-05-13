using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Repository.Interfaces
{
    public interface IStockTransactionsRepository
    {
        Task<IEnumerable<StockTransactions>> GetInfoByStockCodeNameAsync(string name, string stockCode);

        Task<IEnumerable<StockTransactions>> GetAllStockTransactionsAsync();
    }
}