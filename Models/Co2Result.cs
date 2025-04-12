namespace CO2Crawler.Models
{
    public class Co2Result
    {
        public string Url { get; set; } = string.Empty;
        public bool Green { get; set; }
        public double Bytes { get; set; }
        public double GridGrams { get; set; }
        public double RenewableGrams { get; set; }
        public double ChosenGrams { get; set; }
    }
}
