using AccountingBook.Repository;
using FluentScheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public class JobManagerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPDFService _pdfService;
        private readonly IGmailService _mailService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<JobManagerService> _logger;

        public JobManagerService(IServiceProvider serviceProvider,
                                 IPDFService pdfService,
                                 IGmailService mailService,
                                 UserRepository userRepository,
                                 ILogger<JobManagerService> logger)
        {
            _serviceProvider = serviceProvider;
            _pdfService = pdfService;
            _mailService = mailService;
            _userRepository = userRepository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            UpdateStockTransactions();
            // 使用 FluentScheduler 的 JobManager 設定每日 13:30 執行一次的任務
            JobManager.AddJob(() => UpdateStockTransactions(), s => s.ToRunEvery(1).Days().At(13, 30));

            while (!stoppingToken.IsCancellationRequested)
            {
                // 在這裡可以處理其他背景任務的邏輯
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task UpdateStockTransactions()
        {
            var emailList = await _userRepository.GetAllEmailAsync();
            foreach (var email in emailList)
            {
                try
                {
                    var userEmail = email.Email;
                    var userName = await _userRepository.GetUserNamedByUserEmailAsync(userEmail);
                    var filePath = "GmailSource";
                    var numLettrs = 20;
                    var messages = await _mailService.GetMessages(userEmail, numLettrs);

                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            var body = await _mailService.GetMessageBody(userEmail, message.Id);

                            var attachments = await _mailService.GetPdfAttachmentsAsync(userEmail, message.Id);

                            if (attachments.Count > 0)
                            {
                                var pdfResult = await _pdfService.ExtractTextFromPdfAsync(filePath, userName, attachments[0]);
                                if (await _pdfService.SaveTransactionToDatabase(pdfResult))
                                {
                                    _mailService.SendEmail(userEmail, pdfResult);
                                }
                            }
                            else
                            {
                                // Log 沒有 PDF 的訊息
                                _logger.LogWarning($"No PDF attachments found in email with ID: {message.Id} for user: {email.Email}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No messages found for user: {email.Email}");
                    }
                }
                catch (Exception ex)
                {
                    // Log 錯誤訊息
                    _logger.LogError($"Error processing emails for user: {email.Email}. Error: {ex.Message}");
                }
            }
        }
    }
}