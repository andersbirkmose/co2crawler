using CO2Crawler.Models;
using CO2Crawler.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Bind konfiguration til models
        services.Configure<CrawlSettings>(context.Configuration.GetSection("CrawlSettings"));
        services.Configure<EmailSettings>(context.Configuration.GetSection("EmailSettings"));

        // Services
        services.AddSingleton<ReportService>();
        services.AddSingleton<CrawlService>();
        services.AddSingleton<SitemapService>();
        services.AddTransient<WordReportService>();
        



    });

var host = builder.Build();

// Start crawler
var crawlService = host.Services.GetRequiredService<CrawlService>();
await crawlService.RunAsync();

