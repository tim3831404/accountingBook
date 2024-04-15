using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Repository.Interfaces
{
    public interface IUserStockRepository
    {
        Task<IEnumerable<UserStocks>> GetAllUserStocksAsync();
    }
}