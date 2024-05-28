using System;

namespace AccountingBook.Models
{
    public class Dividends
    {
        public int DividendsId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionName { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime DividendTradingDate { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountStock { get; set; }
    }
}