using AccountingBook.Models;
using AccountingBook.Repository;
using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Spire.Pdf;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace AccountingBook.Services
{
    public class PDFService : IPDFService
    {
        private readonly IConfiguration _configuration;
        private readonly UserRepository _userRepository;
        private readonly StockTransactionsRepository _stockTransactionsRepository;
        

        public PDFService(IConfiguration configuration,
                          UserRepository userRepository,
                          StockTransactionsRepository stockTransactionsRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _stockTransactionsRepository = stockTransactionsRepository;
        }

        public async Task<bool> SaveTransactionToDatabase(StockTransactions transaction)
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
                            (TransactionDate, StockCode, StockName, Memo, Withdrawal, Deposit, Balance, TransactionName, PurchasingPrice, Fee, Tax)
                            VALUES
                            (@TransactionDate, @StockCode, @StockName, @Memo, @Withdrawal, @Deposit, @Balance, @TransactionName, @PurchasingPrice, @Fee, @Tax)",
                            transaction);
                }
                else 
                {
                    if (existingTransaction.PurchasingPrice != transaction.PurchasingPrice ||
                        existingTransaction.Fee != transaction.Fee ||
                        existingTransaction.Tax != transaction.Tax)
                    {
                        // Update existing transaction with new values
                        connection.Execute(@"UPDATE StockTransactions
                                    SET PurchasingPrice = @PurchasingPrice,
                                        Fee = @Fee,
                                        Tax = @Tax
                                    WHERE TransactionDate = @TransactionDate
                                    AND StockCode = @StockCode
                                    AND StockName = @StockName
                                    AND TransactionName = @TransactionName",
                                    transaction);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public async Task<StockTransactions> ExtractTextFromPdfAsync(string filePath, string userName, byte[] attachments)
        {
            var allTextBuilder = "";
            var BankSource = string.Empty;
            var transaction = new StockTransactions();

            try
            {
                // 使用 Spire.PDF 讀取 PDF
                PdfDocument pdfDocument = new PdfDocument();
                string password = await _userRepository.GetPasswordByUserNameAsync(userName);
                if (filePath == "GmailSource")
                {
                    pdfDocument.LoadFromBytes(attachments, password);
                }
                else
                {
                    pdfDocument.LoadFromFile(filePath, password);
                }

                PdfTextExtractor CheckBank = new PdfTextExtractor(pdfDocument.Pages[0]);
                PdfTextExtractOptions extractOptions = new PdfTextExtractOptions();
                BankSource += CheckBank.ExtractText(extractOptions);

                if (BankSource.Contains("臺灣集中保管結算所"))
                {
                    foreach (PdfPageBase page in pdfDocument.Pages)
                    {
                        PdfTextExtractor extractedText = new PdfTextExtractor(page);
                        var text = extractedText.ExtractText(extractOptions);
                        text = Regex.Replace(text, ",", "");
                        allTextBuilder+=text;
                    }
                    var t = JsonConvert.SerializeObject(allTextBuilder);
                    //remove watermark
                    t = Regex.Replace(t, @"臺灣集中保管結算所", "");
                    t = Regex.Replace(t, ",", "");
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
                        transaction = new StockTransactions
                        {
                            TransactionDate = new DateTime(int.Parse(TransactionDateParts[0]) + 1911, int.Parse(TransactionDateParts[1]), int.Parse(TransactionDateParts[2])).Date,
                            StockCode = match.Groups[2].Value,
                            StockName = match.Groups[3].Value,
                            Memo = match.Groups[4].Value,
                            Withdrawal = string.IsNullOrWhiteSpace(match.Groups[5].Value) ? 0 : int.Parse(match.Groups[5].Value),
                            Deposit = string.IsNullOrWhiteSpace(match.Groups[6].Value) ? 0 : int.Parse(match.Groups[6].Value),
                            Balance = string.IsNullOrWhiteSpace(match.Groups[7].Value) ? 0 : int.Parse(match.Groups[7].Value),
                            TransactionName = TransactionName,
                            PurchasingPrice = null,
                            Fee = null,
                            Tax = null,
                        };

                        if (transaction.Memo.Contains("買進") || transaction.Memo.Contains("劃撥配發"))
                        {
                            transaction.Deposit = transaction.Withdrawal;
                            transaction.Withdrawal = 0;
                        }
                        // 存入資料庫
                        SaveTransactionToDatabase(transaction);
                    }
                    pdfDocument.Close();
                }
                else if (BankSource.Contains("國泰世華"))
                {
                    Dictionary<string, string> StockCodeDic = new Dictionary<string, string>();
                    Dictionary<string, int> StockBlanceDic = new Dictionary<string, int>();

                    foreach (PdfPageBase page in pdfDocument.Pages)
                    {
                        PdfTextExtractor extractedText = new PdfTextExtractor(page);
                        var text = extractedText.ExtractText(extractOptions);
                        text = Regex.Replace(text, ",", "");
                        allTextBuilder += text;
                    }
                    var t = JsonConvert.SerializeObject(allTextBuilder);
                    string patternOrder = @"\d{4}/\d{2}/\d{2}\s+(\S+)\s+(\S+)\s+(\d+)\s+(\d+\.\d+)\s+(\d+)\s+(\d+)\s+(\d+)";
                    string patternStock = @"(\d+)\s+(\S+)\s+(\d+\.\d+)\s+(\d+)";
                    var matcheOrder = Regex.Matches(t, patternOrder);
                    var matcheStock = Regex.Matches(t, patternStock);

                    foreach (Match match in matcheStock)
                    {
                        StockCodeDic.Add(match.Groups[2].Value, match.Groups[1].Value);
                        StockBlanceDic.Add(match.Groups[2].Value, int.Parse(match.Groups[4].Value));
                    }

                    foreach (Match match in matcheOrder)
                    {
                        var TransactionDateParts = match.Groups[0].Value.Split()[0];
                        var SplitMatch = match.Groups[0].Value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        var StockName = SplitMatch[1];
                        var Memo = SplitMatch[2];
                        var Withdrawal = SplitMatch[3];
                        var Fee = int.Parse(SplitMatch[6]);
                        var Tax = int.Parse(SplitMatch[7]);
                        var PurchasingPrice = decimal.Parse(SplitMatch[4]);
                        var StockCode = StockCodeDic[StockName];
                        if (StockCode == null)
                        {
                            StockCode = _stockTransactionsRepository.GetStockCodeByStockNameAsync(StockName).ToString();
                        }

                        var Balance = 0;
                        if (StockBlanceDic.ContainsKey(StockName))
                        {
                            Balance = StockBlanceDic[StockName];
                        }

                        transaction = new StockTransactions
                        {
                            TransactionDate = new DateTime(int.Parse(TransactionDateParts.Split("/")[0]),
                                                           int.Parse(TransactionDateParts.Split("/")[1]),
                                                           int.Parse(TransactionDateParts.Split("/")[2])).Date,
                            StockName = StockName,
                            StockCode = StockCode,
                            Memo = Memo,
                            Withdrawal = string.IsNullOrWhiteSpace(Withdrawal) ? 0 : int.Parse(Withdrawal),
                            Balance = Balance,
                            TransactionName = userName,
                            PurchasingPrice = PurchasingPrice,
                            Fee = Fee,
                            Tax = Tax,
                        };

                        if (transaction.Memo.Contains("集買"))
                        {
                            transaction.Deposit = transaction.Withdrawal;
                            transaction.Withdrawal = 0;
                        }
                        // 存入資料庫
                        //SaveTransactionToDatabase(transaction);
                    }
                    pdfDocument.Close();
                }
                else if(BankSource.Contains("新光證券"))
                {
                    Dictionary<string, string> StockCodeDic = new Dictionary<string, string>();
                    Dictionary<string, int> StockBlanceDic = new Dictionary<string, int>();

                    foreach (PdfPageBase page in pdfDocument.Pages)
                    {
                        PdfTextExtractor extractedText = new PdfTextExtractor(page);
                        allTextBuilder += extractedText.ExtractText(extractOptions);
                        allTextBuilder = Regex.Replace(allTextBuilder, ",", "");
                        allTextBuilder = JsonConvert.SerializeObject(allTextBuilder);
                        
                    }
                    
                    string patternOrder = @"\d{2}/\d{2}\s+(\d+)\s+(\S+)\s+(\d+)\s+(\d+\.\d+)\s+(\S+)\s+(\d+)\s+(\d+)\s+(\d+)";
                    string patternStock = @"(\d{4,})\s+(\S+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)";
                    string patternDeliveryDate = @"以下是\s+(\d{4})\s+(\S+)\s+(\d{2})\s+(\S+)\s+(\d{2})\s+(\S+)";
                    var matcheOrder = Regex.Matches(allTextBuilder, patternOrder);
                    var matcheStock = Regex.Matches(allTextBuilder, patternStock);
                    var DeliveryDate = Regex.Matches(allTextBuilder, patternDeliveryDate);

                    foreach (Match match in matcheStock)
                    {
                        var num = 0;
                        var conversionSuccessful = int.TryParse(match.Groups[2].Value, out num);
                        if (!int.TryParse(match.Groups[2].Value, out num))
                        {
                            StockCodeDic.Add(match.Groups[2].Value, match.Groups[1].Value);
                            StockBlanceDic.Add(match.Groups[2].Value, int.Parse(match.Groups[3].Value));
                        }
                    };

                    foreach (Match match in matcheOrder)
                    {
                        var TransactionDateParts = match.Groups[0].Value.Split()[0];
                        var SplitMatch = match.Groups[0].Value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        var StockCode = SplitMatch[1];
                        var StockName = SplitMatch[2];
                        var Memo = SplitMatch[5];
                        var Withdrawal = int.Parse(SplitMatch[3]);
                        var PurchasingPrice = decimal.Parse(SplitMatch[4]);
                        var Balance = 0;
                        if (StockBlanceDic.ContainsKey(StockName))
                        {
                            Balance = StockBlanceDic[StockName];
                        }
                        var Fee = int.Parse(SplitMatch[7]);
                        var Tax = int.Parse(SplitMatch[8]);
                        transaction = new StockTransactions
                        {
                            TransactionDate = new DateTime(int.Parse(DeliveryDate[0].Value.Split(" ")[1]),
                                                           int.Parse(TransactionDateParts.Split("/")[0]),
                                                           int.Parse(TransactionDateParts.Split("/")[1])).Date,
                            StockName = StockName,
                            StockCode = StockCode,
                            Memo = Memo,
                            Withdrawal = Withdrawal,
                            Balance = Balance,
                            TransactionName = userName,
                            PurchasingPrice = PurchasingPrice,
                            Fee = Fee,
                            Tax = Tax,
                        };

                        if (transaction.Memo.Contains("現買"))
                        {
                            transaction.Deposit = transaction.Withdrawal;
                            transaction.Withdrawal = 0;
                        }
                        // 存入資料庫
                        //SaveTransactionToDatabase(transaction);
                    }

                    pdfDocument.Close();
                }
            }
            catch (Exception ex)
            {
            }
            return transaction;
        }
    }
}