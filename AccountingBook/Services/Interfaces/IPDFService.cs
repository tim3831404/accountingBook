using System.Net.Mail;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public interface IPDFService
    {
        Task<string> ExtractTextFromPdfAsync(string filePath, string userName,byte[] attachments);
    }
}