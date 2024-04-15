using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Services.Interfaces
{
    public interface IUpdateStockService
    {
        Task<decimal> UpdateColesingkPricesAsync(string stockCode);
    }
}