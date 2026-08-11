using Microsoft.AspNetCore.Mvc.RazorPages;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Pages.Zerberuz;

public sealed class VersionModel : PageModel
{
    private readonly IProfileRuleStore store;

    public VersionModel(IProfileRuleStore store)
    {
        this.store = store;
    }

    public RuleSetDefinition? RuleSet { get; private set; }

    public async Task OnGetAsync(string profile, string version, CancellationToken cancellationToken)
    {
        RuleSet = await store.GetRuleSetAsync(profile, version, cancellationToken).ConfigureAwait(false);
    }

    public string GetTargetSummary(RuleDefinition rule)
    {
        var parts = new List<string> { rule.Target.SymbolKind.ToString() };
        AddIfPresent(parts, "name", rule.Target.NameMustMatch);
        AddIfPresent(parts, "namespace", rule.Target.NamespaceMustMatch);
        AddIfPresent(parts, "path", rule.Target.PathMustMatch);

        return string.Join(" | ", parts);
    }

    public string GetConditionSummary(RuleDefinition rule)
    {
        var parts = new List<string>();
        AddIfPresent(parts, "starts", rule.Condition.MustStartWith);
        AddIfPresent(parts, "ends", rule.Condition.MustEndWith);
        AddIfPresent(parts, "contains", rule.Condition.MustContain);
        AddIfPresent(parts, "match", rule.Condition.MustMatch);
        AddIfPresent(parts, "not match", rule.Condition.MustNotMatch);
        AddIfPresent(parts, "path match", rule.Condition.PathMustMatch);
        AddIfPresent(parts, "path not match", rule.Condition.PathMustNotMatch);
        AddIfPresent(parts, "namespace contains", rule.Condition.NamespaceMustContain);

        return parts.Count == 0
            ? "Sin condicion declarada"
            : string.Join(" | ", parts);
    }

    public string GetHelpTitle(string diagnosticId)
    {
        return RuleSet?.Help.FirstOrDefault(help =>
                string.Equals(help.DiagnosticId, diagnosticId, StringComparison.Ordinal))?.Title
            ?? "Sin ayuda asociada";
    }

    private static void AddIfPresent(ICollection<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value}");
        }
    }
}
