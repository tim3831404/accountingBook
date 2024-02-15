using AccountingBook.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace AccountingBook.Services
{
    public class PDFTransactionParser
    {
        public List<PdfTransaction> ParseTransactions(string text)
        {
            List<PdfTransaction> transactions = new List<PdfTransaction>();

            // 此正則表達式可根據實際情況進行調整
            string pattern = @"(\d{3} \d{2} \d{2})\s+(\d{4})\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)";
            Regex regex = new Regex(pattern);

            MatchCollection matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                PdfTransaction transaction = new PdfTransaction
                {
                    TransactionDate = DateTime.ParseExact(match.Groups[1].Value, "yyy MM dd", null),
                    SecuritiesCode = match.Groups[2].Value,
                    SecuritiesName = match.Groups[3].Value,
                    Memo = match.Groups[4].Value,
                    Withdrawal = decimal.Parse(match.Groups[5].Value),
                    Deposit = decimal.Parse(match.Groups[6].Value),
                    Balance = decimal.Parse(match.Groups[7].Value),
                    AccountNo = match.Groups[8].Value
                };

                transactions.Add(transaction);
            }

            return transactions;
        }
    }
}

