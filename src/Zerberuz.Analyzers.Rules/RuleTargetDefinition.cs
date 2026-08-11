namespace Zerberuz.Analyzers.Rules;

public sealed class RuleTargetDefinition
{
    public ZerberuzSymbolKind SymbolKind { get; set; } = ZerberuzSymbolKind.NamedType;

    public string? NameMustMatch { get; set; }

    public string? NamespaceMustMatch { get; set; }

    public string? PathMustMatch { get; set; }
}
