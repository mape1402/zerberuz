namespace Zerberuz.Analyzers.Rules;

public sealed class RuleConditionDefinition
{
    public string? MustStartWith { get; set; }

    public string? MustEndWith { get; set; }

    public string? MustContain { get; set; }

    public string? MustMatch { get; set; }

    public string? MustNotMatch { get; set; }

    public string? PathMustMatch { get; set; }

    public string? PathMustNotMatch { get; set; }

    public string? NamespaceMustContain { get; set; }
}
