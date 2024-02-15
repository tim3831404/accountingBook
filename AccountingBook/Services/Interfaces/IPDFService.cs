using System.Collections.Generic;

namespace AccountingBook.Services
{
    public interface IPDFService
    {
        
        string ExtractTextFromPdf(string filePath, string password);
           
    }
}
