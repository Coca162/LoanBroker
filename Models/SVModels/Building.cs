using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

public class Building
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("throughputFactor")]
    public double ThroughputFactor { get; set; }

    [JsonPropertyName("efficiency")]
    public double Efficiency { get; set; }

    [JsonPropertyName("recipe")]
    public Recipe Recipe { get; set; }
}
