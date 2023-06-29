using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

public class BaseEntity
{
    [JsonPropertyName("entityType")]
    public EntityType EntityType { get; }
}
