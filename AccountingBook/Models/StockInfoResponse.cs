using System.Collections.Generic;

namespace AccountingBook.Models
{
    public class StockInfoResponse
    {
        public List<StockInfoItem> msgArray { get; set; }
        public string referer { get; set; }
        public int userDelay { get; set; }
        public string rtcode { get; set; }
        public QueryTime queryTime { get; set; }
        public string rtmessage { get; set; }
        public string exKey { get; set; }
        public int cachedAlive { get; set; }
   
    }
}
