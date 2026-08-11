using System.Text.Json;
using System.Text.Json.Serialization;
using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Analyzers.Configuration;

public sealed class RuleSetCacheLoader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public RuleSetDefinition? Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RuleSetDefinition>(json, JsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
