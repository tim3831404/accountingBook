using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AccountingBook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionOverviewController : ControllerBase
    {
        private readonly ITransactionOverviewService _transactionOverviewService;

        public TransactionOverviewController(ITransactionOverviewService transactionOverviewService)
        {
            _transactionOverviewService = transactionOverviewService;
        }

        //[HttpGet]
        //public IActionResult GetProfitAndLoss(DateTime startDate, DateTime endDate, string userName)
        //{
        //    try
        //    {
                
        //        var profitAndLoss = _transactionOverviewService.CalculateProfitAndLoss(startDate, endDate, userName);

        //        // Return the result as JSON
        //        return Ok(profitAndLoss);
        //    }
        //    catch (Exception ex)
        //    {
        //        // If an error occurs, return a 500 Internal Server Error with the error message
        //        return StatusCode(500, $"An error occurred: {ex.Message}");
        //    }
        //}
    }

}
