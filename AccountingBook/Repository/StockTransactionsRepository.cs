using AccountingBook.Models;
using AccountingBook.Repository.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AccountingBook.Repository
{
    public class StockTransactionsRepository : IStockTransactionsRepository
    {
        private readonly IDbConnection _dbConnection;

        public StockTransactionsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<StockTransactions>> GetAllStockTransactionsAsync()
        {
            return await _dbConnection.QueryAsync<StockTransactions>("SELECT TOP 10000 * FROM StockTransactions ORDER BY TransactionDate");
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
            var stockCode = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                "SELECT StockCode FROM StockTransactions WHERE StockName = @stockName",
                new { stockName = stockName }
            );

            return stockCode;
        }

        public async Task<IEnumerable<StockTransactions>> GetInfoByStockCodeNameAsync(string name, string stockCode)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM StockTransactions WHERE 1=1");
            if (!string.IsNullOrEmpty(name))
            {
                queryBuilder.Append(" AND TransactionName = @Name");
            }

            if (!string.IsNullOrEmpty(stockCode))
            {
                queryBuilder.Append(" AND StockCode = @StockCode");
            }

            return await _dbConnection.QueryAsync<StockTransactions>(queryBuilder.ToString(), new { Name = name, StockCode = stockCode });
        }

        public async Task<IEnumerable<StockTransactions>> GetInfoByProfitAsync()
        {
            // 使用 Dapper 執行參數化查詢
            return await _dbConnection.QueryAsync<StockTransactions>(
                "SELECT * FROM StockTransactions WHERE StockCode = @StockCode AND Profit IS NULL ORDER BY TransactionDate, TransactionId",
                new { StockCode = "8085" });
        }

        public async Task UpdateIncomeProfitAsync(int depositTransactionId)
        {
            string query = @"UPDATE StockTransactions SET Profit = 0 WHERE TransactionId = @TransactionId";
            await _dbConnection.ExecuteAsync(query, new { TransactionId = depositTransactionId });
        }

        public async Task UpdateOutcomeProfitAsync(int withdrawalTransactionId, int profit)
        {
            string query = @"UPDATE StockTransactions SET Profit = @Profit WHERE TransactionId = @TransactionId";
            await _dbConnection.ExecuteAsync(query, new { Profit = profit, TransactionId = withdrawalTransactionId });
        }

        public async Task<IEnumerable<StockTransactions>> GetInfoByDateAndUserAsync(DateTime startDate, DateTime endDate, String userName)
        {
            if (userName == null)
            {
                return await _dbConnection.QueryAsync<StockTransactions>(
                    "SELECT * FROM StockTransactions WHERE TransactionDate >= @StartDate AND TransactionDate <= @EndDate",
                    new { startDate = startDate, endDate = endDate });
            }
            else
            {
                return await _dbConnection.QueryAsync<StockTransactions>(
                    "SELECT * FROM StockTransactions WHERE TransactionDate >= @StartDate AND TransactionDate <= @EndDate AND UserName = @userName",
                    new { startDate = startDate, endDate = endDate, userName = userName });
            }
        }
    }
}