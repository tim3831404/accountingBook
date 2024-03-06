using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Interfaces
{
    public interface IUserStockService
    {
        Task<IEnumerable<UserStocks>> GetAllUserStocksAsync();
    }
}