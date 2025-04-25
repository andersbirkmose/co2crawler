namespace CO2Crawler.Models
{
    public class PageResult
    {
        public string Url { get; set; } = string.Empty;
        public bool Green { get; set; }
        public double Bytes { get; set; }

        // Nye felter til indholdstyper
        public double HtmlBytes { get; set; }
        public double ImageBytes { get; set; }
        public double JsBytes { get; set; }
        public double CssBytes { get; set; }
        public double VideoBytes { get; set; }

        public double GridGrams { get; set; }
        public double RenewableGrams { get; set; }
        public double ChosenGrams { get; set; }
    }
}
