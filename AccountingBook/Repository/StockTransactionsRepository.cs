using AccountingBook.Interfaces;
using AccountingBook.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AccountingBook.Repository
{
    public class StockTransactionsRepository
    {
        private readonly IDbConnection _dbConnection;

        public StockTransactionsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<StockTransactions>> GetAllUsersAsync()
        {
            return await _dbConnection.QueryAsync<StockTransactions>("SELECT * FROM Users");
        }

        public async Task<IEnumerable<StockTransactions>> GetAllEmailAsync()
        {
            return await _dbConnection.QueryAsync<StockTransactions>("SELECT Email FROM Users");
        }

        public async Task<string> GetStockCodeByStockNameAsync(string stockName)
        {
            // 使用 Dapper 執行參數化查詢
            var password = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                "SELECT StockCode FROM StockTransactions WHERE StockName = @stockName",
                new { stockName = stockName }
            );

            return password;
        }
    }
}