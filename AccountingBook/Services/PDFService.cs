using Spire.Pdf;
using Spire.Pdf.Utilities;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using AccountingBook.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Globalization;
using AccountingBook.Repository;
using System.Threading.Tasks;


namespace AccountingBook.Services
{
    public class PDFService : IPDFService
    {
        private readonly IConfiguration _configuration;
        private readonly UserRepository _userRepository;
        public PDFService(IConfiguration configuration,
                          UserRepository userRepository) 
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public void SaveTransactionToDatabase(StockTransactions transaction)
        {
            string connectionString = _configuration.GetConnectionString("StockDatabase");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var existingTransaction = connection.QueryFirstOrDefault<StockTransactions>(
                @"SELECT * FROM StockTransactions 
                WHERE TransactionDate = @TransactionDate 
                AND StockCode = @StockCode 
                AND StockName = @StockName 
                AND TransactionName = @TransactionName",
                transaction);
                if (existingTransaction == null) 
                {
                    connection.Execute(@"INSERT INTO StockTransactions 
                                    (TransactionDate, StockCode, StockName, Memo, Withdrawal, Deposit, Balance, TransactionName)
                                    VALUES 
                                    (@TransactionDate, @StockCode, @StockName, @Memo, @Withdrawal, @Deposit, @Balance, @TransactionName)",
                                    transaction);
                }
                
                
            }
        }
        public async Task<string> ExtractTextFromPdfAsync(string filePath, string userName)
        {
            var text = string.Empty;
            var BankSource = string.Empty;

            try
            {
                // 使用 Spire.PDF 讀取 PDF
                PdfDocument pdfDocument = new PdfDocument();
                string password = await _userRepository.GetPasswordByUserNameAsync(userName);
                pdfDocument.LoadFromFile(filePath, password);
                PdfTextExtractor CheckBank = new PdfTextExtractor(pdfDocument.Pages[0]);
                PdfTextExtractOptions extractOptions = new PdfTextExtractOptions();
                BankSource += CheckBank.ExtractText(extractOptions);

                if (BankSource.Contains("臺灣集中保管結算所"))
                {
                    foreach (PdfPageBase page in pdfDocument.Pages)
                    {

                        PdfTextExtractor extractedText = new PdfTextExtractor(page);
                        extractOptions.IsExtractAllText = false;
                        text += extractedText.ExtractText(extractOptions);
                        var t = JsonConvert.SerializeObject(text);
                        //remove watermark
                        t = Regex.Replace(t, @"臺灣集中保管結算所", "");
                        string patternAccount = @"帳號：([^\\n]+)";
                        var matchAccount = Regex.Matches(t, patternAccount);
                        var AccountInfo = matchAccount[0].Groups[1].Value;
                        var patternQuery = $@"{AccountInfo} (.+)";
                        var matchQueryDateTime = Regex.Matches(t, patternQuery);
                        var QueryDate = matchQueryDateTime[0].Value.Split(" ")[1];
                        var QueryTime = matchQueryDateTime[0].Value.Split(" ")[2];
                        // format t content
                        t = Regex.Replace(t, @"臺灣集中保管結算所", "");
                        t = Regex.Replace(t, $@"{AccountInfo}", "");
                        t = Regex.Replace(t, $@"{QueryDate}", "");
                        t = Regex.Replace(t, $@"{QueryTime}", "");
                        //get data
                        string pattern2 = @"戶名：(.+)";
                        var matches1 = Regex.Matches(t, pattern2);
                        string TransactionName = matches1[0].Groups[1].Value.Split("\\")[0];
                        string pattern3 = @"(\d{3} \d{2} \d{2})\s+(\d+)\s+(\S+)\s+(\S+)\s+(\d+|\s+)\s+(\d+|\s+)\s+(\d+)";
                        var matches2 = Regex.Matches(t, pattern3);
                        foreach (Match match in matches2)
                        {


                            var TransactionDateParts = match.Groups[1].Value.Split();
                            var transaction = new StockTransactions
                            {
                                TransactionDate = new DateTime(int.Parse(TransactionDateParts[0]) + 1911, int.Parse(TransactionDateParts[1]), int.Parse(TransactionDateParts[2])).Date,
                                StockCode = match.Groups[2].Value,
                                StockName = match.Groups[3].Value,
                                Memo = match.Groups[4].Value,
                                Withdrawal = string.IsNullOrWhiteSpace(match.Groups[5].Value) ? 0 : int.Parse(match.Groups[5].Value),
                                Deposit = string.IsNullOrWhiteSpace(match.Groups[6].Value) ? 0 : int.Parse(match.Groups[6].Value),
                                Balance = string.IsNullOrWhiteSpace(match.Groups[7].Value) ? 0 : int.Parse(match.Groups[7].Value),
                                TransactionName = TransactionName
                            };

                            if (transaction.Memo.Contains("買進"))
                            {
                                transaction.Deposit = transaction.Withdrawal;
                                transaction.Withdrawal = 0;
                            }
                            // 存入資料庫
                            SaveTransactionToDatabase(transaction);
                        }
                    }
                    pdfDocument.Close();
                }
            
            if (BankSource.Contains("國泰世華"))
            {
                foreach (PdfPageBase page in pdfDocument.Pages)
                {

                    PdfTextExtractor extractedText = new PdfTextExtractor(page);
                    text += extractedText.ExtractText(extractOptions);
                    var t = JsonConvert.SerializeObject(text);
                    string pattern = @"\d{4}/\d{2}/\d{2}\s+\S+\s+\S+\s+\d+\s+\d+\.\d+\s+\d+,\d+\s+\d+\s+\d+\s+\d+";
                    var matches = Regex.Matches(t, pattern);
                    foreach (Match match in matches)
                    {
                        var TransactionDateParts = match.Groups[1].Value.Split();
                        var transaction = new StockTransactions
                        {
                            TransactionDate = new DateTime(int.Parse(TransactionDateParts[0]), int.Parse(TransactionDateParts[1]), int.Parse(TransactionDateParts[2])).Date,
                            StockCode = match.Groups[2].Value,
                            StockName = match.Groups[3].Value,
                            Memo = match.Groups[4].Value,
                            Withdrawal = string.IsNullOrWhiteSpace(match.Groups[5].Value) ? 0 : int.Parse(match.Groups[5].Value),
                            Deposit = string.IsNullOrWhiteSpace(match.Groups[6].Value) ? 0 : int.Parse(match.Groups[6].Value),
                            Balance = string.IsNullOrWhiteSpace(match.Groups[7].Value) ? 0 : int.Parse(match.Groups[7].Value),
                            TransactionName = userName
                        };

                        if (transaction.Memo.Contains("買進"))
                        {
                            transaction.Deposit = transaction.Withdrawal;
                            transaction.Withdrawal = 0;
                        }
                        // 存入資料庫
                        SaveTransactionToDatabase(transaction);
                    }
                }
                pdfDocument.Close();

            }
        }
            catch (Exception ex)
            {

            }
            return text;
        }
                    
                    
        }

    }

