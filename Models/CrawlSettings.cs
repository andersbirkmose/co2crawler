namespace CO2Crawler.Models;

public class CrawlSettings
{
    public string Domain { get; set; } = string.Empty;
    public int MaxConcurrency { get; set; } = 5;
    public List<string> ExcludePaths { get; set; } = new();
}
