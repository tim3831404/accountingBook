using AccountingBook.Models;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public interface IPDFService
    {
        Task<StockTransactions> ExtractTextFromPdfAsync(string filePath, string userName, byte[] attachments);
        Task<bool> SaveTransactionToDatabase(StockTransactions transaction);
    }
}