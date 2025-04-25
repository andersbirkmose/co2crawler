using System.Diagnostics;
using System.Text.Json;
using CO2Crawler.Models;

namespace CO2Crawler.Services;

public static class NodeInterop
{
    public static async Task<Co2Result?> RunCo2JsAsync(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"co2Runner.mjs \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null) return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine($"❌ Node.js-fejl for {url}:\n{error}");
            return null;
        }

        try
        {
            var json = JsonDocument.Parse(output).RootElement;

            var result = new Co2Result
            {
                Url = json.GetProperty("url").GetString() ?? url,
                Green = json.GetProperty("green").GetBoolean(),
                Bytes = (int)json.GetProperty("bytes").GetDouble(),
                HtmlBytes = (int)json.GetProperty("htmlBytes").GetDouble(),
                ImageBytes = (int)json.GetProperty("imageBytes").GetDouble(),
                JsBytes = (int)json.GetProperty("jsBytes").GetDouble(),
                CssBytes = (int)json.GetProperty("cssBytes").GetDouble(),
                VideoBytes = (int)json.GetProperty("videoBytes").GetDouble(),
                Co2 = new Co2Details
                {
                    Grid = new Emission { Grams = json.GetProperty("co2").GetProperty("grid").GetProperty("grams").GetDouble() },
                    Renewable = new Emission { Grams = json.GetProperty("co2").GetProperty("renewable").GetProperty("grams").GetDouble() },
                    Chosen = new Emission { Grams = json.GetProperty("co2").GetProperty("chosen").GetProperty("grams").GetDouble() }
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fejl ved analyse af {url}: {ex.Message}");
            return null;
        }
    }
}
