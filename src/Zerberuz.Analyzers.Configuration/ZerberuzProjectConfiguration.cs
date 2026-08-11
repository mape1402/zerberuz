using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zerberuz.Analyzers.Configuration;

public sealed class ZerberuzProjectConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public string Team { get; set; } = "default";

    public string Profile { get; set; } = "default";

    public string RulesVersion { get; set; } = "latest-compatible";

    public string Mode { get; set; } = "latest-compatible";

    public string RulesEndpoint { get; set; } = "https://rules.zerberuz.dev";

    public string? CacheRoot { get; set; }

    public static ZerberuzProjectConfiguration Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ZerberuzProjectConfiguration();
        }

        return JsonSerializer.Deserialize<ZerberuzProjectConfiguration>(json, JsonOptions)
            ?? new ZerberuzProjectConfiguration();
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return options;
    }
}
