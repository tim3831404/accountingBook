// 在 Services 目錄中建立 UserStockService.cs
using AccountingBook.Interfaces;
using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public class UserStockService : IUserStockService
    {
        private readonly IUserStockRepository _userStockRepository;

        public UserStockService(IUserStockRepository userStockRepository)
        {
            _userStockRepository = userStockRepository;
        }

        public async Task<IEnumerable<UserStocks>> GetAllUserStocksAsync()
        {
            return await _userStockRepository.GetAllUserStocksAsync();
        }
    }
}