using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

public class EntityBalanceRecord
{
    [JsonPropertyName("entityId")]
    public long EntityId { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("taxableBalance")]
    public decimal TaxableBalance { get; set; }
}
