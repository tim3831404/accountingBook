using System;

namespace AccountingBook.Models
{
    public class StockTransactions
    {
        public int TransactionId { get; set; }
        public int UserStockId { get; set; }
        public string TransactionType { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal TransactionPrice { get; set; }
        public int Quantity { get; set; }
    }
}
