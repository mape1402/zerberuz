using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Pages.Zerberuz;

public sealed class PublishModel : PageModel
{
    private readonly IProfileRuleStore store;

    public PublishModel(IProfileRuleStore store)
    {
        this.store = store;
    }

    [BindProperty]
    public string Profile { get; set; } = string.Empty;

    [BindProperty]
    public string RulesVersion { get; set; } = string.Empty;

    [BindProperty]
    public string MinimumEngineVersion { get; set; } = "1.0.0";

    [BindProperty]
    public IList<RuleInput> Rules { get; set; } = new List<RuleInput>();

    public string Message { get; private set; } = string.Empty;

    public bool Succeeded { get; private set; }

    public void OnGet()
    {
        Profile = "backend";
        RulesVersion = "2026.10.01";
        MinimumEngineVersion = "1.0.0";
        Rules =
        [
            new RuleInput
            {
                Id = "ZBZ001",
                Type = ZerberuzRuleType.Naming,
                Title = "Interfaces must start with I",
                Severity = ZerberuzDiagnosticSeverity.Warning,
                SymbolKind = ZerberuzSymbolKind.Interface,
                MustStartWith = "I",
                Message = "Interface names must start with I.",
                HelpMarkdown = """
                ## Summary
                Interfaces are easier to scan when they follow the team naming convention.

                ## Fix
                - Rename the interface so it starts with `I`.
                - Update references.

                ```csharp
                public interface ICustomerRepository { }
                ```
                """,
                HelpSummary = "Interfaces are easier to scan when they follow the team naming convention.",
                HelpWhy = "Consistent names reduce friction during reviews and refactors.",
                HelpTrigger = "An interface name does not start with I.",
                BadExample = "public interface CustomerRepository { }",
                GoodExample = "public interface ICustomerRepository { }",
                FixSteps = "Rename the interface so it starts with I.\nUpdate references.",
                SuppressionGuidance = "Suppress only when external contracts force a different name."
            }
        ];
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        var ruleSet = CreateRuleSetFromForm();
        if (ruleSet is null)
        {
            return;
        }

        var validation = new RuleSetValidator().Validate(ruleSet);
        if (!validation.IsValid)
        {
            Succeeded = false;
            Message = "Validation failed: " + string.Join(" ", validation.Errors.Select(error => $"{error.Code}: {error.Message}"));
            return;
        }

        var result = await store.PublishRuleSetAsync(ruleSet, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            Succeeded = false;
            Message = result.ErrorMessage;
            return;
        }

        Succeeded = true;
        Message = $"Published {ruleSet.Profile}@{ruleSet.RulesVersion}.";
    }

    private RuleSetDefinition? CreateRuleSetFromForm()
    {
        var rules = Rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Id))
            .ToArray();

        if (rules.Length == 0)
        {
            Succeeded = false;
            Message = "Add at least one rule before publishing.";
            return null;
        }

        return new RuleSetDefinition
        {
            SchemaVersion = "1.0",
            RulesVersion = RulesVersion,
            Profile = Profile,
            MinimumEngineVersion = MinimumEngineVersion,
            Rules = rules.Select(rule => new RuleDefinition
            {
                Id = rule.Id.Trim(),
                Type = rule.Type,
                Title = rule.Title.Trim(),
                Severity = rule.Severity,
                Target = new RuleTargetDefinition
                {
                    SymbolKind = rule.SymbolKind,
                    NameMustMatch = EmptyToNull(rule.TargetNameMustMatch),
                    NamespaceMustMatch = EmptyToNull(rule.TargetNamespaceMustMatch),
                    PathMustMatch = EmptyToNull(rule.TargetPathMustMatch)
                },
                Condition = new RuleConditionDefinition
                {
                    MustStartWith = EmptyToNull(rule.MustStartWith),
                    MustEndWith = EmptyToNull(rule.MustEndWith),
                    MustContain = EmptyToNull(rule.MustContain),
                    MustMatch = EmptyToNull(rule.MustMatch),
                    MustNotMatch = EmptyToNull(rule.MustNotMatch),
                    PathMustMatch = EmptyToNull(rule.PathMustMatch),
                    PathMustNotMatch = EmptyToNull(rule.PathMustNotMatch),
                    NamespaceMustContain = EmptyToNull(rule.NamespaceMustContain)
                },
                Message = rule.Message.Trim(),
                HelpUrl = EmptyToNull(rule.HelpUrl)
            }).ToList(),
            Help = rules.Select(rule => new DiagnosticHelpDefinition
            {
                DiagnosticId = rule.Id.Trim(),
                Title = string.IsNullOrWhiteSpace(rule.HelpTitle) ? rule.Title.Trim() : rule.HelpTitle.Trim(),
                Markdown = rule.HelpMarkdown.Trim(),
                Summary = rule.HelpSummary.Trim(),
                Why = rule.HelpWhy.Trim(),
                Trigger = rule.HelpTrigger.Trim(),
                BadExample = rule.BadExample.Trim(),
                GoodExample = rule.GoodExample.Trim(),
                FixSteps = SplitLines(rule.FixSteps),
                SuppressionGuidance = rule.SuppressionGuidance.Trim(),
                RelatedDiagnostics = SplitCsv(rule.RelatedDiagnostics)
            }).ToList()
        };
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IList<string> SplitLines(string value)
    {
        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static IList<string> SplitCsv(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    public sealed class RuleInput
    {
        public string Id { get; set; } = string.Empty;

        public ZerberuzRuleType Type { get; set; } = ZerberuzRuleType.Naming;

        public string Title { get; set; } = string.Empty;

        public ZerberuzDiagnosticSeverity Severity { get; set; } = ZerberuzDiagnosticSeverity.Warning;

        public ZerberuzSymbolKind SymbolKind { get; set; } = ZerberuzSymbolKind.NamedType;

        public string TargetNameMustMatch { get; set; } = string.Empty;

        public string TargetNamespaceMustMatch { get; set; } = string.Empty;

        public string TargetPathMustMatch { get; set; } = string.Empty;

        public string MustStartWith { get; set; } = string.Empty;

        public string MustEndWith { get; set; } = string.Empty;

        public string MustContain { get; set; } = string.Empty;

        public string MustMatch { get; set; } = string.Empty;

        public string MustNotMatch { get; set; } = string.Empty;

        public string PathMustMatch { get; set; } = string.Empty;

        public string PathMustNotMatch { get; set; } = string.Empty;

        public string NamespaceMustContain { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string HelpUrl { get; set; } = string.Empty;

        public string HelpTitle { get; set; } = string.Empty;

        public string HelpMarkdown { get; set; } = string.Empty;

        public string HelpSummary { get; set; } = string.Empty;

        public string HelpWhy { get; set; } = string.Empty;

        public string HelpTrigger { get; set; } = string.Empty;

        public string BadExample { get; set; } = string.Empty;

        public string GoodExample { get; set; } = string.Empty;

        public string FixSteps { get; set; } = string.Empty;

        public string SuppressionGuidance { get; set; } = string.Empty;

        public string RelatedDiagnostics { get; set; } = string.Empty;
    }
}
