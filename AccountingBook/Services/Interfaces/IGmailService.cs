using System.Threading.Tasks;

namespace AccountingBook.Services.Interfaces
{
    public interface IGmailService
    {
        Task<string> ReadEmailsAsync();
        Task SendEmailAsync(string to, string subject, string body);
    }
}
