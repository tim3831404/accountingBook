using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using AccountingBook.Services;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class PDFUploadController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IPDFService _pdfService;
    public PDFUploadController(IWebHostEnvironment webHostEnvironment,
                               IPDFService pdfService)
    {
        _webHostEnvironment = webHostEnvironment;
        _pdfService = pdfService;
    }

    [HttpPost]
    public IActionResult UploadPDF(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            try
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "stock", file.FileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                // 處理 PDF
                string extractedText = _pdfService.ExtractTextFromPdf(filePath, "H124468495");


                return Ok(new { Message = $"檔案 {file.FileName} 已經成功上傳", Status = "Success" });
            }
            catch (Exception ex)
            {
                
                return BadRequest(new { Message = $"上傳失敗: {ex.Message}", Status = "Error" });
            }
        }

        return BadRequest(new { Message = "上傳失敗: 請選擇有效的檔案", Status = "Error" });
    }
}
