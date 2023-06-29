using System.Text.Json.Serialization;

public class DBConfig
{
    public static DBConfig instance;

    public string Host { get; set; }

    public string Password { get; set; }

    public string Username { get; set; }

    public string Database { get; set; }

    public DBConfig()
    {
        // Set main instance to the most recently created config
        instance = this;
    }
}

public class ValourConfig
{
    public static ValourConfig instance;

    [JsonPropertyName("botpassword")]
    public string BotPassword { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("prefix")]
    public List<string> Prefix { get; set; }

    [JsonPropertyName("prod")]
    public bool Production { get; set; }

    public ValourConfig()
    {
        // Set main instance to the most recently created config
        instance = this;
    }
}

public class SVConfig
{
    public static SVConfig instance;

    public string GroupApiKey { get; set; }
    public string LoanBrokerAPIKey { get; set; }

    public SVConfig()
    {
        // Set main instance to the most recently created config
        instance = this;
    }
}