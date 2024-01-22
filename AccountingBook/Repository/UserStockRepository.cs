﻿// 在 Repository 目錄中建立 UserStockRepository.cs
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AccountingBook.Interfaces;
using AccountingBook.Models;
using Dapper;

namespace AccountingBook.Repository
{
    public class UserStockRepository : IUserStockRepository
    {
        private readonly IDbConnection _dbConnection;

        public UserStockRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<UserStocks>> GetAllUserStocksAsync()
        {
            return await _dbConnection.QueryAsync<UserStocks>("SELECT * FROM UserStocks");
        }
    }
}