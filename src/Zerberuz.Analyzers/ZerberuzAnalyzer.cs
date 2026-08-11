using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Zerberuz.Analyzers.Configuration;
using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ZerberuzAnalyzer : DiagnosticAnalyzer
{
    public const string InterfaceNamingDiagnosticId = "ZBZ001";

    private static readonly DiagnosticDescriptor InterfaceNamingDescriptor = new(
        InterfaceNamingDiagnosticId,
        "Interfaces must follow the configured naming convention",
        "Interface '{0}' must follow the configured naming convention",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Interfaces must follow the naming convention declared in the Zerberuz rule cache.",
        helpLinkUri: "https://docs.zerberuz.dev/diagnostics/ZBZ001");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(InterfaceNamingDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(startContext =>
        {
            var ruleSet = LoadRuleSet(startContext.Options, startContext.CancellationToken);
            var namingRules = NamingRuleState.Create(ruleSet);
            if (namingRules.Count == 0)
            {
                return;
            }

            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, namingRules),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, IReadOnlyCollection<NamingRuleState> rules)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        if (namedType.TypeKind != TypeKind.Interface)
        {
            return;
        }

        foreach (var rule in rules)
        {
            if (rule.IsMatch(namedType.Name))
            {
                continue;
            }

            var location = namedType.Locations.FirstOrDefault(candidate => candidate.IsInSource);
            var message = string.IsNullOrWhiteSpace(rule.Message)
                ? string.Format(InterfaceNamingDescriptor.MessageFormat.ToString(), namedType.Name)
                : rule.Message.Replace("{symbolName}", namedType.Name);

            context.ReportDiagnostic(Diagnostic.Create(
                InterfaceNamingDescriptor,
                location,
                namedType.Name,
                message));
        }
    }

    private static RuleSetDefinition? LoadRuleSet(AnalyzerOptions options, CancellationToken cancellationToken)
    {
        var cacheFile = options.AdditionalFiles.FirstOrDefault(IsRulesCacheFile);
        if (cacheFile is null)
        {
            return null;
        }

        var sourceText = cacheFile.GetText(cancellationToken);
        return sourceText is null
            ? null
            : new RuleSetCacheLoader().Load(sourceText.ToString());
    }

    private static bool IsRulesCacheFile(AdditionalText file)
    {
        return string.Equals(Path.GetFileName(file.Path), "rules-cache.json", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NamingRuleState
    {
        private NamingRuleState(string message, string? mustStartWith, Regex? mustMatch)
        {
            Message = message;
            MustStartWith = mustStartWith;
            MustMatch = mustMatch;
        }

        public string Message { get; }

        private string? MustStartWith { get; }

        private Regex? MustMatch { get; }

        public bool IsMatch(string name)
        {
            if (!string.IsNullOrWhiteSpace(MustStartWith) &&
                !name.StartsWith(MustStartWith, StringComparison.Ordinal))
            {
                return false;
            }

            return MustMatch is null || MustMatch.IsMatch(name);
        }

        public static IReadOnlyCollection<NamingRuleState> Create(RuleSetDefinition? ruleSet)
        {
            if (ruleSet is null)
            {
                return Array.Empty<NamingRuleState>();
            }

            return ruleSet.Rules
                .Where(rule =>
                    rule.Id == InterfaceNamingDiagnosticId &&
                    rule.Type == ZerberuzRuleType.Naming &&
                    rule.Target.SymbolKind == ZerberuzSymbolKind.Interface)
                .Select(Create)
                .Where(rule => rule is not null)
                .Cast<NamingRuleState>()
                .ToArray();
        }

        private static NamingRuleState? Create(RuleDefinition rule)
        {
            var regex = CreateRegex(rule.Condition.MustMatch);
            return new NamingRuleState(rule.Message, rule.Condition.MustStartWith, regex);
        }

        private static Regex? CreateRegex(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            return new Regex(
                pattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }
    }
}
