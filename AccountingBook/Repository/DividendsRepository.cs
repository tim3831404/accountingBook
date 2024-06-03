using AccountingBook.Models;
using AccountingBook.Repository.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Mvc;
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

        //public async Task<IEnumerable<Dividends>> FilterDividendsByTransIds(List<int> TransationId)
        //{
        //    if (TransationId == null || TransationId.Count == 0)
        //    {
        //        return await _dbConnection.QueryAsync<Dividends>("SELECT TOP 10000 * FROM Dividends ORDER BY TransactionDate");
        //    }
        //    var query = @"SELECT TOP 10000 * FROM Dividends
        //          WHERE DividendsId NOT IN @Ids
        //          ORDER BY TransactionDate";
        //    return await _dbConnection.QueryAsync<Dividends>(query, new { Ids = TransationId });
        //}
    }
}