using System.Text.Json.Serialization;
using Shared;
public class Config : DefaultConfig
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("prefix")]
    public string[] Prefix { get; set; }
}
