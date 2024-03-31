using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public interface IPDFService
    {
        Task<List<StockTransactions>> ExtractTextFromPdfAsync(string filePath, string userName, byte[] attachments);
        Task<bool> SaveTransactionToDatabase(List<StockTransactions> transactions);
    }
}