using AccountingBook.Interfaces;
using AccountingBook.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AccountingBook.Repository
{
    public class StockRepository : IStockRepository
    {
        private readonly IDbConnection _dbConnection;

        public StockRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Stocks>> GetAllStocksAsync()
        {
            return await _dbConnection.QueryAsync<Stocks>("SELECT * FROM Stocks");
        }
    }
}