using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

[JsonDerivedType(typeof(Group), 0)]
[JsonDerivedType(typeof(Group), 1)]
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

    [JsonPropertyName("taxAbleBalance")]
    public decimal TaxableBalance { get; set; }
}
