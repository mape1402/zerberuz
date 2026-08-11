using System.Text.Json;
using System.Text.Json.Serialization;
using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

internal static class RuleSetJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string Serialize(RuleSetDefinition ruleSet)
    {
        return JsonSerializer.Serialize(ruleSet, JsonOptions);
    }

    public static RuleSetDefinition? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<RuleSetDefinition>(json, JsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
