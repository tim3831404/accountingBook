namespace AccountingBook.Models
{
    public class Stocks
    {
        public int StockId { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public decimal ClosingPrice { get; set; }
    }
}