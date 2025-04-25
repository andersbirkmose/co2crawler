using CO2Crawler.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System.Globalization;

namespace CO2Crawler.Services;

public class ReportService
{
    private readonly EmailSettings _emailSettings;
    private readonly CrawlSettings _crawlSettings;
    private readonly WordReportService _reportService;
    private readonly string _reportFormat;

    public ReportService(IOptions<EmailSettings> emailSettings, IOptions<CrawlSettings> crawlSettings, IOptions<ReportOptions> reportOptions, WordReportService reportService)
    {
        _emailSettings = emailSettings.Value;
        _crawlSettings = crawlSettings.Value;
        _reportService = reportService;
        _reportFormat = reportOptions.Value.Format?.ToLower() ?? "html";
    }

    public async Task GenerateAndSendReportAsync(List<PageResult> results)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var domain = new Uri(_crawlSettings.Domain).Host.Replace("www.", "").Replace(".", "-");
        var fileName = $"co2-report-{domain}-{timestamp}.csv";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<PageResultMap>();
            csv.WriteHeader<PageResult>();
            csv.NextRecord();
            csv.WriteRecords(results);
        }

        Console.WriteLine($"📄 CSV-rapport genereret: {filePath}");
        await SendEmailWithAttachmentAsync(filePath, results, domain);
    }

    private async Task SendEmailWithAttachmentAsync(string filePath, List<PageResult> results, string domain)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_emailSettings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(_emailSettings.Recipient));
        message.Subject = $"CO₂-rapport for {domain}";

        var builder = new BodyBuilder();
        var reportContent = _reportService.GenerateReport(results, _reportFormat);

        if (_reportFormat == "html")
            builder.HtmlBody = reportContent;
        else
            builder.TextBody = reportContent;

        builder.Attachments.Add(filePath);
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, false);
        await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        Console.WriteLine($"📧 E-mail sendt til: {_emailSettings.Recipient}");
    }
}

public class PageResultMap : ClassMap<PageResult>
{
    public PageResultMap()
    {
        Map(m => m.Url);
        Map(m => m.Green);
        Map(m => m.Bytes);
        Map(m => m.HtmlBytes);
        Map(m => m.ImageBytes);
        Map(m => m.JsBytes);
        Map(m => m.CssBytes);
        Map(m => m.VideoBytes);
        Map(m => m.GridGrams);
        Map(m => m.RenewableGrams);
        Map(m => m.ChosenGrams);
    }
}

public class ReportOptions
{
    public string? Format { get; set; }
}
