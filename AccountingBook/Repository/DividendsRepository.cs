using AccountingBook.Models;
using AccountingBook.Repository.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingBook.Repository
{
    public class DividendsRepository : IDividendsRepository
    {
        private readonly IDbConnection _dbConnection;

        public DividendsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<HashSet<int>> GetTransationIdInDividends()
        {
            var transactionIds = await _dbConnection.QueryAsync<int>("SELECT TransactionId FROM Dividends ORDER BY TransactionDate");
            return new HashSet<int>(transactionIds);
        }

        public async Task<List<Dividends>> FilterDividendsByPaymentDate(DateTime startDate, DateTime endDate)
        {
            return await _dbConnection.QueryAsync<Dividends>(
                "SELECT * FROM Dividends WHERE PaymentDate <= @endDate AND PaymentDate >= @startDate ORDER BY StockCode",
                new { startDate = startDate, endDate = endDate });
        }
    }
}