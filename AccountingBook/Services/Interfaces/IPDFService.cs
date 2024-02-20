using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Services
{
    public interface IPDFService
    {

        Task<string> ExtractTextFromPdfAsync(string filePath, string userName);
           
    }
}
