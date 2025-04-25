namespace CO2Crawler.Models;

public class Co2Result
{
    public string Url { get; set; } = "";
    public bool Green { get; set; }
    public int Bytes { get; set; }
    public int HtmlBytes { get; set; }
    public int ImageBytes { get; set; }
    public int JsBytes { get; set; }
    public int CssBytes { get; set; }
    public int VideoBytes { get; set; }
    public Co2Details Co2 { get; set; } = new();
}

public class Co2Details
{
    public Emission Grid { get; set; } = new();
    public Emission Renewable { get; set; } = new();
    public Emission Chosen { get; set; } = new();
}

public class Emission
{
    public double Grams { get; set; }
}
