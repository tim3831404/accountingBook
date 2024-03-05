using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Util;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2.Responses;
using System.Linq;
using System.Net.Http;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using AccountingBook.Repository;
using Spire.Pdf.Texts;
using Spire.Pdf;

public class MailService : IGmailService
{
    private readonly string ApplicationName = "AccountingBook";
    private readonly string SecretFilePath = @"D:\ASP\AccountingBook\Secret";
    string RedirectUri = $"https://localhost:5001/api/Gmail";
    string Username = "k3831404@gmail.com";

    

    public async Task<string> GetAuthUrl()
    {

        string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose};
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

    public async Task<List<Message>> GetMessages(string userId)
    {
        var service = GetGmailService();

        var listRequest = service.Users.Messages.List(userId);
        //listRequest.LabelIds = "INBOX"; // 指定搜尋收件匣
        listRequest.Q = "label:Accounting";
        listRequest.MaxResults = 10; // 最多取得 10 封信件（根據需求調整）

        var messages = await listRequest.ExecuteAsync();
        return messages?.Messages.ToList();
    }

    public async Task<string> GetMessageBody(string userId, string messageId)
    {
        var service = GetGmailService();

        var message = await service.Users.Messages.Get(userId, messageId).ExecuteAsync();
        var body = message?.Payload?.Body?.Data;
        var Snippet = message.Snippet?.ToString();

        if (!string.IsNullOrEmpty(Snippet))
        {
            return message.Snippet;
        }
        else if(!string.IsNullOrEmpty(body))
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
                    pdfAttachments.Add( Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/')));
                    PdfDocument pdfDocument = new PdfDocument();
                    //string password = await _userRepository.GetPasswordByUserNameAsync(userName);
                    pdfDocument.LoadFromBytes(pdfAttachments[0], "H124468495");
                    PdfTextExtractor CheckBank = new PdfTextExtractor(pdfDocument.Pages[0]);
                    PdfTextExtractOptions extractOptions = new PdfTextExtractOptions();
                    BankSource += CheckBank.ExtractText(extractOptions);
                    //var data = Base64UrlEncoder.Decode(attachment.Data);
                    //pdfAttachments.Add(data);
                }
            }
        }

        return pdfAttachments;
    }


}
