using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
public class GmailController : Controller
{
    private readonly IGmailService _mailService;

    public GmailController(IGmailService mailService)
    {
        _mailService = mailService;
    }

    [HttpGet("GetAuth")]
    public async Task<IActionResult> GetAuthUrl()
    {
        string authUrl = await _mailService.GetAuthUrl();
        return Ok(new { AuthUrl = authUrl });
    }

    [HttpPost("GetToken")]
    public async Task<IActionResult> AuthReturn([FromBody] AuthorizationCodeResponseUrl AuthUrl)
    {
        string result = await _mailService.AuthReturn(AuthUrl);
        return Ok(new { Result = result });
    }

    [HttpGet("GetMessages")]
    public async Task<IActionResult> GetTopTenMessages()
    {
        try
        {
            var userId = "yan6216@gmail.com"; // 替換為實際的 Gmail 地址
            var numLetters = 10;
            var messages = await _mailService.GetMessages(userId, numLetters);

            if (messages != null)
            {
                var result = new List<string>();

                foreach (var message in messages)
                {
                    var body = await _mailService.GetMessageBody(userId, message.Id);
                    result.Add(body);
                }

                return Ok(result);
            }
            else
            {
                return NotFound("No messages found.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }

    [HttpGet("GetTopTenAttachmentsInfo")]
    public async Task<IActionResult> GetTopTenAttachmentsInfo()
    {
        try
        {
            string userId = "k3831404@gmail.com"; // 替換為實際的 Gmail 地址
            var numLetters = 10;
            var messages = await _mailService.GetMessages(userId, numLetters);

            if (messages != null)
            {
                var result = new List<(string Body, List<string> Attachments)>();

                foreach (var message in messages) // 取前十封郵件
                {
                    var body = await _mailService.GetMessageBody(userId, message.Id);
                    var attachments = await _mailService.GetAttachmentsInfoAsync(userId, message.Id);

                    result.Add((body, attachments));
                }

                return Ok(result);
            }
            else
            {
                return NotFound("No messages found.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }

    [HttpGet("GetPdfAttachments")]
    public async Task<IActionResult> GetPdfAttachmentsAsync()
    {
        try
        {
            var userId = "k3831404@gmail.com";
            var numLettrs = 10;
            var messages = await _mailService.GetMessages(userId, numLettrs);

            if (messages != null)
            {
                var result = new List<byte[]>();

                foreach (var message in messages) // 取前十封郵件
                {
                    var body = await _mailService.GetMessageBody(userId, message.Id);
                    var attachments = await _mailService.GetPdfAttachmentsAsync(userId, message.Id);

                    //result.Add(attachments);
                }

                return Ok(result);
            }
            else
            {
                return NotFound("No messages found.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }
}