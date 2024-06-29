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
        public DateTime? PaymentDate { get; set; }
        public DateTime? DividendTradingDate { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountStock { get; set; }
        public int TransactionId { get; set; }

        public void copy(StockTransactions info)
        {
            TransactionDate = info.TransactionDate.Date;
            TransactionName = info.TransactionName;
            StockCode = info.StockCode;
            StockName = info.StockName;
            PaymentDate = null;
            DividendTradingDate = null;
            AmountCash = 0;
            AmountStock = 0;
            TransactionId = info.TransactionId;
        }
    }
    public class DividendsCaucalInfos
    {
        public Dividends basic { get; set; }
        public int count { get; set; }
    }
}