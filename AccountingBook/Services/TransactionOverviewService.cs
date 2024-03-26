using AccountingBook.Repository;
using AccountingBook.Services.Interfaces;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

//namespace AccountingBook.Services
//{
//    public class TransactionOverviewService : ITransactionOverviewService
//    {
//        private readonly StockTransactionsRepository _stockTransactionsRepository;


//        public TransactionOverviewService(StockTransactionsRepository stockTransactionsRepository)
//        {
//            _stockTransactionsRepository = stockTransactionsRepository;
//        }

        

    //        public async Task<Dictionary<string, decimal>> CalculateProfit2(DateTime startDate, DateTime endDate, string userName)
    //    {
            
    //        var transactions = await _stockTransactionsRepository.GetInfoByDateAndUserAsync(startDate, endDate, userName);

    //        // Initialize dictionaries to store total purchase cost and total sales revenue for each stock
    //        var totalPurchaseCosts = new Dictionary<string, decimal>();
    //        var totalSalesRevenues = new Dictionary<string, decimal>();

            
    //        foreach (var transaction in transactions)
    //        {
    //            if (transaction.Withdrawal != 0)
    //            {
    //                var transactions
    //            }

                
    //        }

    //        // Calculate the profit/loss for each stock
    //        var profitAndLoss = new Dictionary<string, decimal>();
    //        foreach (var stockName in totalPurchaseCosts.Keys)
    //        {
    //            // Calculate the profit/loss for the stock
    //            decimal profitLoss = totalSalesRevenues[stockName] - totalPurchaseCosts[stockName];
    //            profitAndLoss[stockName] = profitLoss;
    //        }

    //        return profitAndLoss;
    //    }
    //}

    
        
//}
