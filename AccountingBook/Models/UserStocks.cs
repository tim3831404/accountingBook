using System;

namespace AccountingBook.Models
{
    public class UserStocks
    {
        public int UserStockId { get; set; }
        public int UserId { get; set; }
        public int StockId { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int Quantity { get; set; }
    }
}
