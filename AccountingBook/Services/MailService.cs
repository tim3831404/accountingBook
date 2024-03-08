using AccountingBook.Repository;
using AccountingBook.Services;
using FluentScheduler;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose };
        using (var stream =
            new FileStream(Path.Combine(SecretFilePath, "client_secret.json"), FileMode.Open, FileAccess.Read))
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
        string[] scopes = new[] { GmailService.Scope.GmailReadonly };

        using (var stream = new FileStream(Path.Combine(SecretFilePath, "client_secret.json"), FileMode.Open, FileAccess.Read))
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

            //這個動作應該是要把 code 換成 token
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

    private GmailService GetGmailService()
    {
        UserCredential credential = GetUserCredential();
        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        return service;
    }

    private UserCredential GetUserCredential()
    {
        var stream = new FileStream(Path.Combine(SecretFilePath, "client_secret.json"), FileMode.Open, FileAccess.Read);
        var dataStore = new FileDataStore(Path.Combine(SecretFilePath, "Credentials", Username));

        IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
            Scopes = new[] { GmailService.Scope.MailGoogleCom },
            DataStore = dataStore
        });

        var authResult = new AuthorizationCodeWebApp(flow, RedirectUri, Username)
            .AuthorizeAsync(Username, CancellationToken.None).Result;

        return authResult.Credential;
    }

    public async Task<List<Message>> GetMessages(string userId, int numLetters)
    {
        var service = GetGmailService();
        var listRequest = service.Users.Messages.List(userId);
        //listRequest.LabelIds = "INBOX"; // 指定搜尋收件匣
        listRequest.Q = "label:Accounting";
        listRequest.MaxResults = numLetters;
        var messages = await listRequest.ExecuteAsync();
        return messages?.Messages.ToList();
    }

    public async Task<string> GetMessageBody(string userId, string messageId)
    {
        var service = GetGmailService();

        var message = await service.Users.Messages.Get(userId, messageId).ExecuteAsync();
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

    public async Task<List<string>> GetAttachmentsInfoAsync(string userId, string messageId)
    {
        var service = GetGmailService();

        var message = await service.Users.Messages.Get(userId, messageId).ExecuteAsync();
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

    public async Task<List<byte[]>> GetPdfAttachmentsAsync(string userId, string messageId)
    {
        var service = GetGmailService();
        var message = await service.Users.Messages.Get(userId, messageId).ExecuteAsync();
        var pdfAttachments = new List<byte[]>();

        if (message?.Payload?.Parts != null)
        {
            foreach (var part in message.Payload.Parts)
            {
                if (part.MimeType == "application/pdf" && part.Body?.AttachmentId != null)
                {
                    var BankSource = string.Empty;
                    var attachment = service.Users.Messages.Attachments.Get(userId, messageId, part.Body.AttachmentId).Execute();
                    pdfAttachments.Add(Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/')));
                }
            }
        }

        return pdfAttachments;
    }
}

