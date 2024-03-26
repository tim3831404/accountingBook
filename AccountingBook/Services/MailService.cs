using AccountingBook.Models;
using AccountingBook.Repository;
using AccountingBook.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

public class MailService : IGmailService
{
    private readonly string ApplicationName = "AccountingBook";
    private readonly string SecretFilePath = @"D:\ASP\AccountingBook\Secret";
    private string RedirectUri = $"https://localhost:5001/api/gmail/gettoken";
    private string Username = "yan6216@gmail.com";
    private readonly UserRepository _userRepository;
    private readonly IPDFService _pDFService;

    public MailService(UserRepository userRepository,
                      IPDFService PDFService)
    {
        _userRepository = userRepository;
        _pDFService = PDFService;
    }

    public async Task<string> GetAuthUrl()
    {
        string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose, GmailService.Scope.GmailModify };
        using (var stream =
            new FileStream(Path.Combine(SecretFilePath, $"client_secret_{Username}.json"), FileMode.Open, FileAccess.Read))
        {
            string credPath = @"D:\ASP\AccountingBook\token.json";
            FileDataStore dataStore = null;
            var credentialRoot = Path.Combine(SecretFilePath, "Credentials");
            if (!Directory.Exists(credentialRoot))
            {
                Directory.CreateDirectory(credentialRoot);
            }
            string filePath = Path.Combine(credentialRoot, Username);
            dataStore = new FileDataStore(filePath);

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
                Scopes = Scopes,
                DataStore = dataStore
            });

            var authResult = await new AuthorizationCodeWebApp(flow, RedirectUri, Username)
            .AuthorizeAsync(Username, CancellationToken.None);

            return authResult.RedirectUri;
        }
    }

    public async Task<string> AuthReturn(AuthorizationCodeResponseUrl authorizationCode)
    {
        string[] scopes = new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose, GmailService.Scope.GmailModify };

        using (var stream = new FileStream(Path.Combine(SecretFilePath, $"client_secret_{Username}.json"), FileMode.Open, FileAccess.Read))
        {
            //確認 credential 的目錄已建立.
            var credentialRoot = Path.Combine(SecretFilePath, "Credentials");
            if (!Directory.Exists(credentialRoot))
            {
                Directory.CreateDirectory(credentialRoot);
            }

            //暫存憑証用目錄
            string tempPath = Path.Combine(credentialRoot, authorizationCode.State);

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
                Scopes = scopes,
                DataStore = new FileDataStore(tempPath)
            });

            await flow.ExchangeCodeForTokenAsync(Username, authorizationCode.Code, RedirectUri, CancellationToken.None).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(authorizationCode.State))
            {
                string newPath = Path.Combine(credentialRoot, Username);
                if (tempPath.ToLower() != newPath.ToLower())
                {
                    if (Directory.Exists(newPath))
                        Directory.Delete(newPath, true);

                    Directory.Move(tempPath, newPath);
                }
            }

            return "OK";
        }
    }

    private GmailService GetGmailService(string userEmail)
    {
        UserCredential credential = GetUserCredential(userEmail);
        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        return service;
    }

    private UserCredential GetUserCredential(string userEmail)
    {
        var stream = new FileStream(Path.Combine(SecretFilePath, $"client_secret_{userEmail}.json"), FileMode.Open, FileAccess.Read);
        var dataStore = new FileDataStore(Path.Combine(SecretFilePath, "Credentials", userEmail));

        IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
            Scopes = new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose, GmailService.Scope.GmailModify },
            DataStore = dataStore
        });

        var authResult = new AuthorizationCodeWebApp(flow, RedirectUri, userEmail)
            .AuthorizeAsync(userEmail, CancellationToken.None).Result;

        return authResult.Credential;
    }

    public async Task<List<Message>> GetMessages(string userEmail, int numLetters)
    {
        var service = GetGmailService(userEmail);
        var listRequest = service.Users.Messages.List(userEmail);
        //listRequest.LabelIds = "INBOX"; // 指定搜尋收件匣
        listRequest.Q = "label:Stock is:unread";
        //listRequest.MaxResults = numLetters;
        var messages = await listRequest.ExecuteAsync();

        if (messages?.Messages != null && messages.Messages.Any())
        {
            var markAsReadBatch = new BatchRequest(service);

            foreach (var message in messages.Messages)
            {
                var mods = new ModifyMessageRequest { RemoveLabelIds = new List<string> { "UNREAD" } };
                var request = service.Users.Messages.Modify(mods, userEmail, message.Id);
                markAsReadBatch.Queue<Message>(request, (content, error, i, msg) =>
                {
                    if (error != null)
                    {
                        Console.WriteLine($"Error updating message {message.Id}: {error.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"Message {message.Id} marked as read successfully.");
                    }
                });
            }

            await markAsReadBatch.ExecuteAsync(CancellationToken.None);
        }
        return messages?.Messages.ToList();
    }

    public async Task<string> GetMessageBody(string userEmail, string messageId)
    {
        var service = GetGmailService(userEmail);
        //var att = service.Users.Messages.Get(userEmail, messageId);  可以拿attachment ID，未測試拿PDF檔案
        var message = await service.Users.Messages.Get(userEmail, messageId).ExecuteAsync();
        var body = message?.Payload?.Body?.Data;
        var snippet = message.Snippet?.ToString();

        if (!string.IsNullOrEmpty(snippet))
        {
            return message.Snippet;
        }
        else if (!string.IsNullOrEmpty(body))
        {
            var decodedBody = Base64UrlEncoder.Decode(body);
            return decodedBody;
        }
        else if (!string.IsNullOrEmpty(message.Payload.Parts[1].Body.Data))
        {
            var Parts = message.Payload.Parts;
            var res = String.Empty;
            foreach (var Part in Parts)
            {
                var PartData = Part.Body.Data;
                if (!string.IsNullOrEmpty(PartData))
                {
                    var decodedBody = Base64UrlEncoder.Decode(PartData);
                    res += decodedBody;
                }
            }

            return res;
        }

        return null;
    }

    public async Task<List<string>> GetAttachmentsInfoAsync(string userEmail, string messageId)
    {
        var service = GetGmailService(userEmail);

        var message = await service.Users.Messages.Get(userEmail, messageId).ExecuteAsync();

        var attachments = new List<string>();

        if (message?.Payload?.Parts != null)
        {
            foreach (var part in message.Payload.Parts)
            {
                if (!string.IsNullOrEmpty(part.Filename))
                {
                    attachments.Add(part.Filename);
                }
            }
        }

        return attachments;
    }

    public async Task<List<byte[]>> GetPdfAttachmentsAsync(string userEmail, string messageId)
    {
        var service = GetGmailService(userEmail);
        var message = await service.Users.Messages.Get(userEmail, messageId).ExecuteAsync();
        var pdfAttachments = new List<byte[]>();

        if (message?.Payload?.Parts != null)
        {
            foreach (var part in message.Payload.Parts)
            {
                if (part.MimeType == "application/pdf" && part.Body?.AttachmentId != null)

                {
                    var attachment = service.Users.Messages.Attachments.Get(userEmail, messageId, part.Body.AttachmentId).Execute();
                    pdfAttachments.Add(Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/')));
                }
                if (part.Parts != null)
                {
                    foreach (var subPart in part.Parts)
                    {
                        {
                            if (subPart.MimeType == "application/pdf" || subPart.Filename.EndsWith(".pdf"))
                            {
                                var attachment = service.Users.Messages.Attachments.Get(userEmail, messageId, subPart.Body.AttachmentId).Execute();
                                pdfAttachments.Add(Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/')));
                            }
                        }
                    }
                }
            }
        }

        return pdfAttachments;
    }

    public async Task SendEmail(string userEmail, StockTransactions updatedContent)
    {
        try
        {
            var service = GetGmailService(userEmail);
            var subject = "StockTransactions has been updated";
            var body = updatedContent;
            var message = new Message
            {
                Raw = Base64UrlEncode($"From: {userEmail}\r\nTo: {userEmail}\r\nSubject: {subject}\r\n\r\n{body}")
            };

            await service.Users.Messages.Send(message, "me").ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
    private string Base64UrlEncode(string input)
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(inputBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}
