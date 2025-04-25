using System.Xml.Linq;
using CO2Crawler.Models;
using Microsoft.Extensions.Options;

namespace CO2Crawler.Services;

public class SitemapService
{
    private readonly CrawlSettings _crawlSettings;
    private readonly HttpClient _httpClient = new();

    public SitemapService(IOptions<CrawlSettings> crawlSettings)
    {
        _crawlSettings = crawlSettings.Value;
    }

    public async Task<List<string>> GetUrlsFromSitemapAsync()
    {
        try
        {
            var sitemapUrl = new Uri(new Uri(_crawlSettings.Domain), "/sitemap.xml").ToString();
            var response = await _httpClient.GetStringAsync(sitemapUrl);
            var xml = XDocument.Parse(response);

            var urls = xml.Descendants()
                .Where(e => e.Name.LocalName == "loc")
                .Select(e => e.Value.Trim().TrimEnd('/'))
                .Where(url => url.StartsWith(_crawlSettings.Domain))
                .Distinct()
                .ToList();

            return urls;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Kunne ikke hente sitemap: {ex.Message}");
            return new List<string>();
        }
    }
}
