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
    public const string FolderStructureDiagnosticId = "ZBZ100";

    private static readonly DiagnosticDescriptor InterfaceNamingDescriptor = new(
        InterfaceNamingDiagnosticId,
        "Interfaces must follow the configured naming convention",
        "Interface '{0}' must follow the configured naming convention",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Interfaces must follow the naming convention declared in the Zerberuz rule cache.",
        helpLinkUri: "https://docs.zerberuz.dev/diagnostics/ZBZ001");

    private static readonly DiagnosticDescriptor FolderStructureDescriptor = new(
        FolderStructureDiagnosticId,
        "Types must follow the configured folder structure",
        "Type '{0}' must follow the configured folder structure",
        "FolderStructure",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types must be placed in folders declared by the Zerberuz rule cache.",
        helpLinkUri: "https://docs.zerberuz.dev/diagnostics/ZBZ100");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(InterfaceNamingDescriptor, FolderStructureDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(startContext =>
        {
            var ruleSet = LoadRuleSet(startContext.Options, startContext.CancellationToken);
            var namingRules = NamingRuleState.Create(ruleSet);
            var folderRules = FolderStructureRuleState.Create(ruleSet);
            if (namingRules.Count == 0 && folderRules.Count == 0)
            {
                return;
            }

            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, namingRules, folderRules),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        IReadOnlyCollection<NamingRuleState> namingRules,
        IReadOnlyCollection<FolderStructureRuleState> folderRules)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        AnalyzeInterfaceNaming(context, namedType, namingRules);
        AnalyzeFolderStructure(context, namedType, folderRules);
    }

    private static void AnalyzeInterfaceNaming(
        SymbolAnalysisContext context,
        INamedTypeSymbol namedType,
        IReadOnlyCollection<NamingRuleState> rules)
    {
        if (namedType.TypeKind != TypeKind.Interface || rules.Count == 0)
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
            if (IsGeneratedLocation(location))
            {
                return;
            }

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

    private static void AnalyzeFolderStructure(
        SymbolAnalysisContext context,
        INamedTypeSymbol namedType,
        IReadOnlyCollection<FolderStructureRuleState> rules)
    {
        if (namedType.TypeKind != TypeKind.Class || rules.Count == 0)
        {
            return;
        }

        var location = namedType.Locations.FirstOrDefault(candidate => candidate.IsInSource);
        if (IsGeneratedLocation(location))
        {
            return;
        }

        var sourcePath = location?.SourceTree?.FilePath;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        var checkedSourcePath = sourcePath!;
        foreach (var rule in rules)
        {
            if (rule.IsMatch(namedType.Name, checkedSourcePath))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                FolderStructureDescriptor,
                location,
                namedType.Name));
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

    private static bool IsGeneratedLocation(Location? location)
    {
        var path = location?.SourceTree?.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
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

    private sealed class FolderStructureRuleState
    {
        private FolderStructureRuleState(string? nameSuffix, Regex pathPattern)
        {
            NameSuffix = nameSuffix;
            PathPattern = pathPattern;
        }

        private string? NameSuffix { get; }

        private Regex PathPattern { get; }

        public bool IsMatch(string name, string path)
        {
            if (!string.IsNullOrWhiteSpace(NameSuffix) &&
                !name.EndsWith(NameSuffix, StringComparison.Ordinal))
            {
                return true;
            }

            return PathPattern.IsMatch(NormalizePath(path));
        }

        public static IReadOnlyCollection<FolderStructureRuleState> Create(RuleSetDefinition? ruleSet)
        {
            if (ruleSet is null)
            {
                return Array.Empty<FolderStructureRuleState>();
            }

            return ruleSet.Rules
                .Where(rule =>
                    rule.Id == FolderStructureDiagnosticId &&
                    rule.Type == ZerberuzRuleType.FolderStructure &&
                    rule.Target.SymbolKind == ZerberuzSymbolKind.Class &&
                    !string.IsNullOrWhiteSpace(rule.Condition.PathMustMatch))
                .Select(Create)
                .Where(rule => rule is not null)
                .Cast<FolderStructureRuleState>()
                .ToArray();
        }

        private static FolderStructureRuleState? Create(RuleDefinition rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Condition.PathMustMatch))
            {
                return null;
            }

            var pathMustMatch = rule.Condition.PathMustMatch!;
            return new FolderStructureRuleState(
                ReadSuffix(rule.Target.NameMustMatch),
                CreateGlobRegex(pathMustMatch));
        }

        private static string? ReadSuffix(string? pattern)
        {
            if (pattern is null || string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            if (!pattern.StartsWith(".*", StringComparison.Ordinal))
            {
                return null;
            }

            return pattern.Substring(2).TrimEnd('$');
        }

        private static Regex CreateGlobRegex(string glob)
        {
            var normalized = NormalizePath(glob);
            var escaped = Regex.Escape(normalized)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*");

            return new Regex(
                "^" + escaped + "$",
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
