using AccountingBook.Models;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGmailService
{
    Task<string> GetAuthUrl();

    Task<string> AuthReturn(AuthorizationCodeResponseUrl authorizationCode);

    Task<List<Message>> GetMessages(string userEmail, int numLetters);

    Task<string> GetMessageBody(string userId, string messageId);

    Task<List<string>> GetAttachmentsInfoAsync(string userId, string messageId);

    Task<List<byte[]>> GetPdfAttachmentsAsync(string userId, string messageId);
    Task<string> GetMessageSnippetAsync(string userId, string messageId);
    
    Task SendEmail(string userEmail, List<StockTransactions> transaction);
}