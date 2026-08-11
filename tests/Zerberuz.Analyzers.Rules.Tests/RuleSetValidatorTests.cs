using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Analyzers.Rules.Tests;

public sealed class RuleSetValidatorTests
{
    [Fact]
    public void Validate_accepts_rule_set_with_matching_help()
    {
        var ruleSet = new RuleSetDefinition
        {
            RulesVersion = "2026.08.11",
            Profile = "backend",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Message = "Interface '{symbolName}' must start with 'I'.",
                    Target = new RuleTargetDefinition
                    {
                        SymbolKind = ZerberuzSymbolKind.Interface
                    },
                    Condition = new RuleConditionDefinition
                    {
                        MustMatch = "^I[A-Z].*"
                    }
                }
            },
            Help =
            {
                new DiagnosticHelpDefinition
                {
                    DiagnosticId = "ZBZ001",
                    Title = "Interfaces must start with I",
                    Summary = "Interface names must use the configured prefix."
                }
            }
        };

        var result = new RuleSetValidator().Validate(ruleSet);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_rule_without_help()
    {
        var ruleSet = new RuleSetDefinition
        {
            RulesVersion = "2026.08.11",
            Profile = "backend",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Message = "Interface '{symbolName}' must start with 'I'."
                }
            }
        };

        var result = new RuleSetValidator().Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "ZBZV020");
    }

    [Fact]
    public void Validate_rejects_invalid_regex()
    {
        var ruleSet = new RuleSetDefinition
        {
            RulesVersion = "2026.08.11",
            Profile = "backend",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "ZBZ001",
                    Type = ZerberuzRuleType.Naming,
                    Title = "Interfaces must start with I",
                    Message = "Interface '{symbolName}' must start with 'I'.",
                    Condition = new RuleConditionDefinition
                    {
                        MustMatch = "["
                    }
                }
            },
            Help =
            {
                new DiagnosticHelpDefinition
                {
                    DiagnosticId = "ZBZ001",
                    Title = "Interfaces must start with I"
                }
            }
        };

        var result = new RuleSetValidator().Validate(ruleSet);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "ZBZV016");
    }
}
