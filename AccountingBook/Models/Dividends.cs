using System;

namespace AccountingBook.Models
{
    public class Dividends
    {
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public DateTime PaymentDate { get; set; }
        public int AmountCash { get; set; }
        public int AmountStock { get; set; }
    }
}