using System.Text;
using CO2Crawler.Models;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace CO2Crawler.Services;

public class WordReportService
{
    private readonly CrawlSettings _crawlSettings;

    public WordReportService(IOptions<CrawlSettings> crawlSettings)
    {
        _crawlSettings = crawlSettings.Value;
    }

    public string GenerateReport(List<PageResult> results, string format = "html")
    {
        var green = results.All(r => r.Green);
        var totalCo2 = results.Sum(r => r.ChosenGrams);
        var avgCo2 = results.Average(r => r.ChosenGrams);
        var culture = new CultureInfo("da-DK");
        var domain = new Uri(_crawlSettings.Domain).Host;

        string benchmarkConclusion = avgCo2 switch
        {
            > 15 => "Dit website forbruger mere CO₂ end det gennemsnitlige website.",
            >= 13 => "Dit website ligger på niveau med det gennemsnitlige website.",
            >= 1 => "Dit website bruger mindre CO₂ end gennemsnittet, men mere end best practice.",
            >= 0.9 => "Hurra! Dit website bruger cirka som best practice.",
            < 0.9 => "Hurra! Dit website bruger mindre end best practice.",
            _ => ""
        };

        if (format.ToLower() == "text")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CO₂-rapport for {domain}");
            sb.AppendLine($"Genereret: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", culture)}\n");
            sb.AppendLine("Sammenfatning:");
            sb.AppendLine($"Grøn hosting: {(green ? "Ja" : "Nej")}");
            sb.AppendLine($"Antal sider: {results.Count.ToString("N0", culture)}");
            sb.AppendLine($"Samlet CO₂ for hele websitet: {totalCo2.ToString("N2", culture)} gram");
            sb.AppendLine($"Gennemsnit pr. side: {avgCo2.ToString("N2", culture)} gram\n");

            sb.AppendLine("Top 10 tungeste sider:");
            sb.AppendLine("Tabellen viser de tungeste sider på websitet. Det vil være oplagt at tage stilling til disse sider først.");
            foreach (var page in results.OrderByDescending(r => r.Bytes).Take(10))
            {
                sb.AppendLine($"- {page.Url}: {(page.Bytes / 1024.0 / 1024.0).ToString("N2", culture)} MB");
            }

            sb.AppendLine("\nFordeling af indholdstyper (KB):");
            sb.AppendLine("Tabellen viser fordelingen af KB på typer af indhold. Redaktøren kan vurdere om man kan optimere noget af indholdet væk fra video til fx animationer eller billeder som vejer markant mindre.");
            sb.AppendLine($"HTML: {(results.Sum(r => r.HtmlBytes) / 1024.0).ToString("N0", culture)}");
            sb.AppendLine($"CSS: {(results.Sum(r => r.CssBytes) / 1024.0).ToString("N0", culture)}");
            sb.AppendLine($"JS: {(results.Sum(r => r.JsBytes) / 1024.0).ToString("N0", culture)}");
            sb.AppendLine($"BILLEDER: {(results.Sum(r => r.ImageBytes) / 1024.0).ToString("N0", culture)}");
            sb.AppendLine($"VIDEO: {(results.Sum(r => r.VideoBytes) / 1024.0).ToString("N0", culture)}");
            sb.AppendLine("Iframe-videoer fra YouTube, Vimeo, Dreambroker, TwentyThree og VideoTool er estimeret til 5MB pr. video.");

            sb.AppendLine("\nBenchmark:");
            sb.AppendLine($"Din side: {avgCo2.ToString("N2", culture)}g");
            sb.AppendLine("Globalt gennemsnit: 13–15g");
            sb.AppendLine("Best Practice: <1g (<500KB)");
            sb.AppendLine($"{benchmarkConclusion}\n");

            var vask = totalCo2 / 130;
            var biffer = totalCo2 / 4000;
            var te = totalCo2 / 20;
            var mail = totalCo2 / 4;
            var opladninger = totalCo2 / 0.005;
            var søgninger = totalCo2 / 0.25;

            sb.AppendLine($"{totalCo2.ToString("N2", culture)} gram CO₂. Er det meget?");
            sb.AppendLine("Dit samlede website forbruger lige så meget CO₂ som:");
            sb.AppendLine($"🔌 {opladninger.ToString("N0", culture)} mobilopladninger (0,005g pr. opladning)");
            sb.AppendLine($"🔍 {søgninger.ToString("N0", culture)} Google-søgninger (0,25g pr. søgning)");
            sb.AppendLine($"🍵 {te.ToString("N0", culture)} kopper te (20g pr. kop)");
            sb.AppendLine($"📧 {mail.ToString("N0", culture)} e-mails (4g pr. e-mail)");
            sb.AppendLine($"🧺 {vask.ToString("N0", culture)} tøjvaske (130g pr. vask)");
            sb.AppendLine($"🥩 {biffer.ToString("N1", culture)} oksebøffer (4.000g pr. bøf)");

            sb.AppendLine("\nDenne e-mailrapport giver et overblik over websitets CO₂-forbrug. Hvis du har brug for mere information og evt. sammenholde tallene med din besøgsstatistik, er du velkommen til at kontakte mig på afb@kruso.dk.");

            return sb.ToString();
        }

        var html = new StringBuilder();
        html.Append("<html><body style='font-family:sans-serif;'>");
        html.Append($"<h1>CO₂-rapport for {domain}</h1>");
        html.Append($"<p><strong>Genereret:</strong> {DateTime.Now.ToString("yyyy-MM-dd HH:mm", culture)}</p>");

        html.Append("<h2>Sammenfatning</h2>");
        html.Append($"<p><strong>Grøn hosting:</strong> {(green ? "Ja" : "Nej")}<br>");
        html.Append($"<strong>Antal sider:</strong> {results.Count.ToString("N0", culture)}<br>");
        html.Append($"<strong>Samlet CO₂:</strong> {totalCo2.ToString("N2", culture)} gram<br>");
        html.Append($"<strong>Gennemsnit pr. side:</strong> {avgCo2.ToString("N2", culture)} gram</p>");

        html.Append("<h2>Top 10 tungeste sider</h2>");
        html.Append("<p>Tabellen viser de tungeste sider på websitet. Det vil være oplagt at tage stilling til disse sider først.</p>");
        html.Append("<table style='width:100%; border-collapse:collapse;' border='1'><tr style='background-color:#f0f0f0'><th>URL</th><th>Størrelse (MB)</th></tr>");
        foreach (var page in results.OrderByDescending(r => r.Bytes).Take(10))
        {
            html.Append($"<tr><td>{page.Url}</td><td style='text-align:right'>{(page.Bytes / 1024.0 / 1024.0).ToString("N2", culture)}</td></tr>");
        }
        html.Append("</table>");

        html.Append("<h2>Fordeling af indholdstyper (KB)</h2>");
        html.Append("<p>Tabellen viser fordelingen af KB på typer af indhold. Redaktøren kan vurdere om man kan optimere noget af indholdet væk fra video til fx animationer eller billeder som vejer markant mindre.</p>");
        html.Append("<table style='width:100%; border-collapse:collapse;' border='1'><tr style='background-color:#f0f0f0'><th>Type</th><th>KB</th></tr>");
        html.Append($"<tr><td>HTML</td><td style='text-align:right'>{(results.Sum(r => r.HtmlBytes) / 1024.0).ToString("N0", culture)}</td></tr>");
        html.Append($"<tr><td>CSS</td><td style='text-align:right'>{(results.Sum(r => r.CssBytes) / 1024.0).ToString("N0", culture)}</td></tr>");
        html.Append($"<tr><td>JS</td><td style='text-align:right'>{(results.Sum(r => r.JsBytes) / 1024.0).ToString("N0", culture)}</td></tr>");
        html.Append($"<tr><td>BILLEDER</td><td style='text-align:right'>{(results.Sum(r => r.ImageBytes) / 1024.0).ToString("N0", culture)}</td></tr>");
        html.Append($"<tr><td>VIDEO</td><td style='text-align:right'>{(results.Sum(r => r.VideoBytes) / 1024.0).ToString("N0", culture)}</td></tr>");
        html.Append("</table>");
        html.Append("<p><em>Vi forudsætter, at iframe-videoer fra YouTube, Vimeo, Dreambroker, TwentyThree og VideoTool tæller med 5MB pr. video.</em></p>");

        html.Append("<h2>Benchmark: Din side vs. standard</h2>");
        html.Append($"<p>Din side: {avgCo2.ToString("N2", culture)}g<br>");
        html.Append("Globalt gennemsnit: 13–15g<br>");
        html.Append("Best Practice: &lt;1g (&lt;500KB)</p>");
        html.Append($"<p><em>{benchmarkConclusion}</em></p>");

        var vask2 = totalCo2 / 130;
        var biffer2 = totalCo2 / 4000;
        var te2 = totalCo2 / 20;
        var mail2 = totalCo2 / 4;
        var opladninger2 = totalCo2 / 0.005;
        var søgninger2 = totalCo2 / 0.25;

        html.Append($"<h2>{totalCo2.ToString("N2", culture)} gram CO₂. Er det meget?</h2>");
        html.Append("<p>Dit samlede website forbruger lige så meget CO₂ som:</p>");
        html.Append($"<p>🔌 {opladninger2.ToString("N0", culture)} mobilopladninger (0,005g pr. opladning)<br>");
        html.Append($"🔍 {søgninger2.ToString("N0", culture)} Google-søgninger (0,25g pr. søgning)<br>");
        html.Append($"🍵 {te2.ToString("N0", culture)} kopper te (20g pr. kop)<br>");
        html.Append($"📧 {mail2.ToString("N0", culture)} e-mails (4g pr. e-mail)<br>");
        html.Append($"🧺 {vask2.ToString("N0", culture)} tøjvaske (130g pr. vask)<br>");
        html.Append($"🥩 {biffer2.ToString("N1", culture)} oksebøffer (4.000g pr. bøf)</p>");

        html.Append("<p style='margin-top:2em; font-style:italic;'>Denne e-mailrapport giver et overblik over websitets CO₂-forbrug. Hvis du har brug for mere information og evt. sammenholde tallene med din besøgsstatistik, er du velkommen til at kontakte mig på <a href='mailto:afb@kruso.dk'>afb@kruso.dk</a>.</p>");

        html.Append("</body></html>");
        return html.ToString();
    }
}
