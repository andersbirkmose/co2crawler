using System.Net.Http;
using HtmlAgilityPack;
using CO2Crawler.Models;
using Microsoft.Extensions.Options;

namespace CO2Crawler.Services;

public class CrawlService
{
    private readonly ReportService _reportService;
    private readonly CrawlSettings _crawlSettings;
    private readonly SemaphoreSlim _semaphore;
    private readonly HashSet<string> _visited = new();
    private readonly HttpClient _httpClient = new();
    private readonly SitemapService _sitemapService;

    public CrawlService(ReportService reportService, SitemapService sitemapService, IOptions<CrawlSettings> crawlSettings)
    {
        _reportService = reportService;
        _sitemapService = sitemapService;
        _crawlSettings = crawlSettings.Value;
        _semaphore = new SemaphoreSlim(_crawlSettings.MaxConcurrency > 0 ? _crawlSettings.MaxConcurrency : 5);
    }

    public async Task RunAsync()
    {
        var urls = await _sitemapService.GetUrlsFromSitemapAsync();

        if (urls.Count == 0)
        {
            Console.WriteLine("⚠️ Intet sitemap fundet – crawler HTML i stedet");
            urls = await DiscoverUrlsFromHtmlAsync(_crawlSettings.Domain);
        }

        var results = new List<PageResult>();

        foreach (var url in urls)
        {
            await _semaphore.WaitAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_visited.Contains(url)) return;
                    _visited.Add(url);
                    Console.WriteLine($"🔎 Analyserer: {url}");
                    var result = await NodeInterop.RunCo2JsAsync(url);
                    if (result != null)
                    {
                        var page = new PageResult
                        {
                            Url = result.Url,
                            Green = result.Green,
                            Bytes = result.Bytes,
                            HtmlBytes = result.HtmlBytes,
                            ImageBytes = result.ImageBytes,
                            JsBytes = result.JsBytes,
                            CssBytes = result.CssBytes,
                            VideoBytes = result.VideoBytes,
                            GridGrams = result.Co2.Grid.Grams,
                            RenewableGrams = result.Co2.Renewable.Grams,
                            ChosenGrams = result.Co2.Chosen.Grams
                        };
                        results.Add(page);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Undtagelse ved {url}: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }

        while (_semaphore.CurrentCount < (_crawlSettings.MaxConcurrency > 0 ? _crawlSettings.MaxConcurrency : 5))
        {
            await Task.Delay(100);
        }

        await _reportService.GenerateAndSendReportAsync(results);
    }

    private async Task<List<string>> DiscoverUrlsFromHtmlAsync(string baseUrl)
    {
        var discovered = new HashSet<string>();

        try
        {
            var html = await _httpClient.GetStringAsync(baseUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var baseUri = new Uri(baseUrl);

            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>())
            {
                var href = link.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(href)) continue;

                if (href.StartsWith("#") || href.StartsWith("mailto:")) continue;

                try
                {
                    var absoluteUrl = new Uri(baseUri, href).ToString().Split('#')[0].TrimEnd('/');
                    if (!discovered.Contains(absoluteUrl) && absoluteUrl.StartsWith(_crawlSettings.Domain))
                        discovered.Add(absoluteUrl);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fejl ved HTML-analyse: {ex.Message}");
        }

        return discovered.ToList();
    }
}
