using System;

namespace AccountingBook.Models
{
    public class Dividends
    {
        public int DividendId { get; set; }
        public int UserStockId { get; set; }
        public DateTime PaymentDate { get; set; }
        public int AmountCash { get; set; }
        public int AmountStock { get; set; }
    }
}
