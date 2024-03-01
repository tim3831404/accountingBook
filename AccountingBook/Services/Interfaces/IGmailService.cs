using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGmailService
{
    Task<string> GetAuthUrl();
    Task<string> AuthReturn(AuthorizationCodeResponseUrl authorizationCode);
    Task<List<Message>> GetMessages(string userId);
    //List<Message> GetMessages(string userId);
    //string GetMessageBody(string userId, string messageId);
}
