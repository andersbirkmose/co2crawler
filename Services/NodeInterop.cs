using System.Diagnostics;
using System.Text.Json;
using CO2Crawler.Models;

namespace CO2Crawler.Services
{
    public static class NodeInterop
    {
        public static async Task<Co2Result?> RunCo2JsAsync(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"co2Runner.mjs {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    Console.WriteLine($"❌ Kunne ikke starte Node-process for {url}");
                    return null;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"❌ Node.js-fejl for {url}:\n{error.Trim()}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine($"⚠️  Ingen output fra co2Runner.mjs for {url}.");
                    return null;
                }

                var json = JsonDocument.Parse(output);
                var root = json.RootElement;

                return new Co2Result
                {
                    Url = root.GetProperty("url").GetString() ?? url,
                    Green = root.GetProperty("green").GetBoolean(),
                    Bytes = root.GetProperty("bytes").GetDouble(),
                    GridGrams = root.GetProperty("co2").GetProperty("grid").GetProperty("grams").GetDouble(),
                    RenewableGrams = root.GetProperty("co2").GetProperty("renewable").GetProperty("grams").GetDouble(),
                    ChosenGrams = root.GetProperty("co2").GetProperty("chosen").GetProperty("grams").GetDouble()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Undtagelse ved {url}: {ex.Message}");
                return null;
            }
        }
    }
}
