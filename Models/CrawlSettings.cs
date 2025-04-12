namespace CO2Crawler.Models
{
    public class CrawlSettings
    {
        public string Domain { get; set; } = string.Empty;
        public string[] ExcludePaths { get; set; } = Array.Empty<string>();
        public int MaxConcurrentRequests { get; set; } = 5;
    }
}
