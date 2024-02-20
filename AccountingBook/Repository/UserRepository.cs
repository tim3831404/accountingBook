﻿using AccountingBook.Interfaces;
using AccountingBook.Models;
using Dapper;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AccountingBook.Repository
{
    public class UserRepository : IUserService
    {
        private readonly IDbConnection _dbConnection;
        public UserRepository (IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public async Task<IEnumerable<Users>> GetAllUsersAsync()
        {
            return await _dbConnection.QueryAsync<Users>("SELECT * FROM Users");
        }

        public async Task<string> GetPasswordByUserNameAsync(string userName)
        {
            // 使用 Dapper 執行參數化查詢
            var password = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                "SELECT PasswordHash FROM Users WHERE UserName = @UserName",
                new { UserName = userName }
            );

            return password;
        }

    }
}
