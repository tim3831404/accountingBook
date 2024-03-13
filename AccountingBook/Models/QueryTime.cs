namespace AccountingBook.Models
{
    public class QueryTime
    {
        public string sysDate { get; set; }
        public int stockInfoItem { get; set; }
        public int stockInfo { get; set; }
        public string sessionStr { get; set; }
        public string sysTime { get; set; }
        public bool showChart { get; set; }
        public long sessionFromTime { get; set; }
        public long sessionLatestTime { get; set; }
    }
}