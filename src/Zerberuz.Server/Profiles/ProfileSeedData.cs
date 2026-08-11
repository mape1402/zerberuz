using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

internal static class ProfileSeedData
{
    public static RuleSetDefinition CreateBackendRuleSet()
    {
        return new RuleSetDefinition
        {
            SchemaVersion = "1.0",
            RulesVersion = "2026.08.11",
            Profile = "backend",
            MinimumEngineVersion = "1.0.0",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Severity = ZerberuzDiagnosticSeverity.Warning,
                    Target = new RuleTargetDefinition
                    {
                        SymbolKind = ZerberuzSymbolKind.Interface
                    },
                    Condition = new RuleConditionDefinition
                    {
                        MustStartWith = "I",
                        MustMatch = "^I[A-Z].*"
                    },
                    Message = "Interface '{symbolName}' must start with 'I'."
                },
                new RuleDefinition
                {
                    Id = "ZBZ100",
                    Type = ZerberuzRuleType.FolderStructure,
                    Title = "Services must live in a Services folder",
                    Severity = ZerberuzDiagnosticSeverity.Warning,
                    Target = new RuleTargetDefinition
                    {
                        SymbolKind = ZerberuzSymbolKind.Class,
                        NameMustMatch = ".*Service$"
                    },
                    Condition = new RuleConditionDefinition
                    {
                        PathMustMatch = "src/**/Services/**"
                    },
                    Message = "Service class '{symbolName}' must be placed under a Services folder."
                }
            },
            Help =
            {
                new DiagnosticHelpDefinition
                {
                    DiagnosticId = "ZBZ001",
                    Title = "Interfaces must start with I",
                    Summary = "Interface names must use the configured prefix.",
                    Why = "Consistent interface naming improves scanning and makes abstractions easier to identify.",
                    Trigger = "An interface symbol was found whose name does not match the configured naming pattern.",
                    BadExample = "public interface Repository { }",
                    GoodExample = "public interface IRepository { }",
                    FixSteps =
                    {
                        "Rename the interface so it starts with I.",
                        "Update all references.",
                        "Run the test suite after the rename."
                    },
                    SuppressionGuidance = "Suppress only when interoperating with generated code or external naming contracts.",
                    RelatedDiagnostics = { "ZBZ100" }
                },
                new DiagnosticHelpDefinition
                {
                    DiagnosticId = "ZBZ100",
                    Title = "Services must live in a Services folder",
                    Summary = "Service classes should be placed under a Services folder.",
                    Why = "Consistent folder layout makes service boundaries easy to scan.",
                    Trigger = "A class ending in Service was found outside a matching Services path.",
                    BadExample = "src/Orders/OrderService.cs",
                    GoodExample = "src/Orders/Services/OrderService.cs",
                    FixSteps =
                    {
                        "Move the service class under a Services folder.",
                        "Update namespaces if your project maps folders to namespaces.",
                        "Run the test suite after the move."
                    },
                    SuppressionGuidance = "Suppress only when the class name is part of an external or generated contract.",
                    RelatedDiagnostics = { "ZBZ001" }
                }
            }
        };
    }
}
