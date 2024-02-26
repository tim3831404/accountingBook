using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

[Route("api/[controller]")]
[ApiController]
public class GmailController : ControllerBase
{
    private readonly IGmailService _gmailService;

    public GmailController(IGmailService gmailService)
    {
        _gmailService = gmailService;
    }

    [HttpGet("read-emails")]
    public async Task<IActionResult> ReadEmails()
    {
        try
        {
            string result = await _gmailService.ReadEmailsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }
}
