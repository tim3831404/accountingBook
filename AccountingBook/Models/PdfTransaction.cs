using System;
namespace AccountingBook.Models
{
    public class PdfTransaction
    {
        public DateTime TransactionDate { get; set; }
        public string SecuritiesCode { get; set; }
        public string SecuritiesName { get; set; }
        public string Memo { get; set; }
        public decimal Withdrawal { get; set; }
        public decimal Deposit { get; set; }
        public decimal Balance { get; set; }
        public string AccountNo { get; set; }
        
    }
}

