using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Gmail.v1;
using AccountingBook.Services.Interfaces;

public class MailService : IGmailService
{
    private readonly string _credentialsPath;
    private readonly string _tokenPath;

    public MailService(string credentialsPath, string tokenPath)
    {
        _credentialsPath = credentialsPath;
        _tokenPath = tokenPath;
    }

    public async Task<string> ReadEmailsAsync()
    {
        UserCredential credential = await GetCredentialAsync();

        var gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Gmail API",
        });

        // Your logic to read emails goes here
        // Example: ListLabelsResponse labels = await gmailService.Users.Labels.List("me").ExecuteAsync();

        return "Emails read successfully";
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        UserCredential credential = await GetCredentialAsync();

        var gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Gmail API",
        });

        // Your logic to send email goes here
        // Example: Message email = CreateMessage(to, subject, body);
        // await gmailService.Users.Messages.Send(email, "me").ExecuteAsync();

        Console.WriteLine($"Email sent to {to} successfully");
    }

    private async Task<UserCredential> GetCredentialAsync()
    {
        using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend },
                "user",
                CancellationToken.None,
                new FileDataStore(_tokenPath, true));

            return credential;
        }
    }
}
