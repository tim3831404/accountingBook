using FluentScheduler;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using AccountingBook.Repository;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AccountingBook.Services
{
    public class JobManagerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPDFService _pdfService;
        private readonly MailService _mailService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<JobManagerService> _logger;

        public JobManagerService(IServiceProvider serviceProvider,
                                 IPDFService pdfService,
                                 MailService mailService,
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
            // 使用 FluentScheduler 的 JobManager 設定每日 13:30 執行一次的任務
            JobManager.AddJob(() => GetGmailPdf(), s => s.ToRunEvery(1).Days().At(13, 30));

            while (!stoppingToken.IsCancellationRequested)
            {
                // 在這裡可以處理其他背景任務的邏輯
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task GetGmailPdf()
        {
            var emailList = await _userRepository.GetAllEmailAsync();
            foreach (var email in emailList)
            {
                try
                {
                    string userId = email.Email;
                    var filePath = "GmailSource";
                    var numLettrs = 20;
                    var messages = await _mailService.GetMessages(userId, numLettrs);

                    if (messages != null)
                    {

                        foreach (var message in messages)
                        {
                            var body = await _mailService.GetMessageBody(userId, message.Id);
                            var attachments = await _mailService.GetPdfAttachmentsAsync(userId, message.Id);
                            var extractedText = await _pdfService.ExtractTextFromPdfAsync(filePath, email.UserName, attachments[0]);

                            if (attachments.Count > 0)
                            {
                                var extractedText = await _pdfService.ExtractTextFromPdfAsync(filePath, email.UserName, attachments[0]);
                                // 在這裡處理 extractedText
                            }
                            else
                            {
                                // Log 沒有 PDF 的訊息
                                _logger.LogWarning($"No PDF attachments found in email with ID: {message.Id} for user: {email.UserName}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No messages found for user: {email.UserName}");
                    }
                }
                catch (Exception ex)
                {
                    // Log 錯誤訊息
                    _logger.LogError($"Error processing emails for user: {email.UserName}. Error: {ex.Message}");
                }
            }
        }
    }
}
