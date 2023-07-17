using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

public class BaseEntity
{
    [JsonPropertyName("id")]
    public long Id { get; set;}

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("entityType")]
    public EntityType EntityType { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("taxableBalance")]
    public decimal TaxableBalance { get; set; }
}
