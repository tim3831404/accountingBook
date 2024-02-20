﻿using System;

namespace AccountingBook.Models
{
    public class StockTransactions
    {
        public DateTime TransactionDate { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public string Memo { get; set; }
        public int Withdrawal { get; set; }
        public int Deposit { get; set; }
        public int Balance { get; set; }
        public string TransactionName { get; set; }
    }
}
