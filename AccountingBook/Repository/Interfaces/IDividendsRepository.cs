using AccountingBook.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Repository.Interfaces
{
    public interface IDividendsRepository
    {
        Task<HashSet<int>> GetTransationIdInDividends();

        Task<List<Dividends>> FilterDividendsByPaymentDate(DateTime startDate, DateTime endDate);
    }
}