namespace Zerberuz.Analyzers.Rules;

public sealed class RuleSetDefinition
{
    public string SchemaVersion { get; set; } = "1.0";

    public string RulesVersion { get; set; } = string.Empty;

    public string Profile { get; set; } = string.Empty;

    public string MinimumEngineVersion { get; set; } = "1.0.0";

    public IList<RuleDefinition> Rules { get; set; } = new List<RuleDefinition>();

    public IList<DiagnosticHelpDefinition> Help { get; set; } = new List<DiagnosticHelpDefinition>();
}
