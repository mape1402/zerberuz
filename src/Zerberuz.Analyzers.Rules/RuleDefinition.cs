namespace Zerberuz.Analyzers.Rules;

public sealed class RuleDefinition
{
    public string Id { get; set; } = string.Empty;

    public ZerberuzRuleType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public ZerberuzDiagnosticSeverity Severity { get; set; } = ZerberuzDiagnosticSeverity.Warning;

    public RuleTargetDefinition Target { get; set; } = new();

    public RuleConditionDefinition Condition { get; set; } = new();

    public string Message { get; set; } = string.Empty;

    public string? HelpUrl { get; set; }
}
