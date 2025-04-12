using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using CO2Crawler.Models;
using System.Collections.Concurrent;

namespace CO2Crawler.Services
{
    public class CrawlService
    {
        private readonly CrawlSettings _settings;
        private readonly ReportService _reportService;

        public CrawlService(IOptions<CrawlSettings> settings, ReportService reportService)
        {
            _settings = settings.Value;
            _reportService = reportService;
        }

        public async Task CrawlAndReportAsync()
        {
            var visited = new ConcurrentDictionary<string, bool>();
            var queue = new ConcurrentQueue<string>();
            var results = new ConcurrentBag<PageResult>();
            var semaphore = new SemaphoreSlim(_settings.MaxConcurrentRequests);
            var tasks = new List<Task>();

            queue.Enqueue(NormalizeUrl(_settings.Domain));

            while (!queue.IsEmpty || tasks.Any(t => !t.IsCompleted))
            {
                while (queue.TryDequeue(out var url))
                {
                    var normalized = NormalizeUrl(url);

                    if (visited.ContainsKey(normalized) || ShouldExclude(normalized))
                        continue;

                    visited.TryAdd(normalized, true);

                    await semaphore.WaitAsync();
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine($"Analyserer: {normalized}");
                            var html = await new HttpClient().GetStringAsync(normalized);

                            var co2 = await NodeInterop.RunCo2JsAsync(normalized);
                            if (co2 != null)
                            {
                                results.Add(new PageResult
                                {
                                    Url = co2.Url,
                                    Green = co2.Green,
                                    Bytes = co2.Bytes,
                                    GridGrams = co2.GridGrams,
                                    RenewableGrams = co2.RenewableGrams,
                                    ChosenGrams = co2.ChosenGrams
                                });
                            }

                            var doc = new HtmlDocument();
                            doc.LoadHtml(html);
                            var links = doc.DocumentNode.SelectNodes("//a[@href]");

                            if (links != null)
                            {
                                foreach (var link in links)
                                {
                                    var href = link.GetAttributeValue("href", "");
                                    string absoluteUrl;

                                    if (href.StartsWith("/"))
                                        absoluteUrl = new Uri(new Uri(_settings.Domain), href).ToString();
                                    else if (href.StartsWith(_settings.Domain))
                                        absoluteUrl = href;
                                    else
                                        continue;

                                    var cleanHref = NormalizeUrl(absoluteUrl);
                                    if (!visited.ContainsKey(cleanHref) && !ShouldExclude(cleanHref))
                                    {
                                        queue.Enqueue(cleanHref);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Fejl ved analyse af {normalized}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                    await Task.Delay(10);
                }

                await Task.Delay(50);
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("✅ Alle sider analyseret. Genererer rapport...");
            await _reportService.GenerateAndSendReportAsync(results.ToList());
        }

        private bool ShouldExclude(string url)
        {
            return _settings.ExcludePaths.Any(path => url.Contains(path));
        }

        private string NormalizeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

                if (normalized == $"{uri.Scheme}://{uri.Host}")
                    return normalized + "/";

                return normalized;
            }
            catch
            {
                return url;
            }
        }
    }
}
