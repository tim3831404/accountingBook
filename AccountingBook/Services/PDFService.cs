using Spire.Pdf;
using Spire.Pdf.Utilities;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using AccountingBook.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;
using System.Transactions;

namespace AccountingBook.Services
{
    public class PDFService : IPDFService
    {
        public string ExtractTextFromPdf(string filePath, string password)
        {
            string text = string.Empty;
            StringBuilder Pdfsb = new StringBuilder();

            try
            {
                // 使用 Spire.PDF 讀取 PDF
                PdfDocument pdfDocument = new PdfDocument();
                pdfDocument.LoadFromFile(filePath, password);
                PdfTableExtractor extractedTable = new PdfTableExtractor(pdfDocument);
                PdfTable[] tableLists = null;
                // 提取文本
                foreach (PdfPageBase page in pdfDocument.Pages)
                {
                    
                    PdfTextExtractor extractedText = new PdfTextExtractor(page);
                    PdfTextExtractOptions extractOptions = new PdfTextExtractOptions();
                    extractOptions.IsExtractAllText = true;
                    text += extractedText.ExtractText(extractOptions);
                    var t = JsonConvert.SerializeObject(text);
                    string pattern = @"\d{3} \d{2} \d{2}\s+\d+\s+\S+\s+\S+\s+\S+\s+(\d+)\s+(\d+)";
                    var matches = Regex.Matches(t, pattern);

                    // 将匹配结果转换为对象列表
                    tableLists = extractedTable.ExtractTable(pdfDocument.Pages.IndexOf(page));
                    if (tableLists != null && tableLists.Length > 0) 
                    { 
                        foreach (PdfTable table in tableLists) 
                        {
                            int row =   table.GetRowCount();
                            int column = table.GetColumnCount();
                            for (int i = 0; i < row; i++) 
                            {
                                for (int j = 0; j < column; j++)
                                {
                                    string tabletext = table.GetText(i, j);
                                    Pdfsb.Append(tabletext + " ");
                                }
                            }
                        }
                    }
                }


                pdfDocument.Close();
            }
            catch (Exception ex)
            {
                // 處理錯誤，例如輸出到日誌或報告給使用者
                Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
            }

            return text;
        }

        // 其他可能的實現...
    }
}
