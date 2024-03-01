using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
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
    [HttpGet("auth")]
    public async Task<IActionResult> GetAuthUrl()
    {
        string authUrl = await _mailService.GetAuthUrl();
        return Ok(new { AuthUrl = authUrl });
    }

    [HttpPost]
    public async Task<IActionResult> AuthReturn([FromBody]AuthorizationCodeResponseUrl AuthUrl)
    {
        string result = await _mailService.AuthReturn(AuthUrl);
        return Ok(new { Result = result });
    }

    //[HttpGet]
    //public async Task<List<Message>> GetMessages()
    //{
    //    string res = await _mailService.GetMessages();
    //    return Ok(new { res = res });
    //}
    //[HttpGet]
    //public IActionResult GetMessages()
    //{
    //    try
    //    {
    //        // 使用 gmailService 進行收信操作
    //        var messages = _gmailService.GetMessages("me");

    //        List<object> result = new List<object>();

    //        foreach (var message in messages)
    //        {
    //            var messageId = message.Id;
    //            var messageBody = _gmailService.GetMessageBody("me", messageId);

    //            var messageData = new
    //            {
    //                MessageId = messageId,
    //                MessageBody = messageBody
    //            };

    //            result.Add(messageData);
    //        }

    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest($"Error: {ex.Message}");
    //    }
    //}
}
