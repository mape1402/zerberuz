using System.Text.RegularExpressions;

namespace Zerberuz.Analyzers.Rules;

public sealed class RuleSetValidator
{
    public RuleSetValidationResult Validate(RuleSetDefinition? ruleSet)
    {
        var errors = new List<RuleSetValidationError>();

        if (ruleSet is null)
        {
            errors.Add(new RuleSetValidationError("ZBZV001", "Rule set is required."));
            return new RuleSetValidationResult(errors);
        }

        Require(ruleSet.SchemaVersion, "ZBZV002", "Schema version is required.", errors);
        Require(ruleSet.RulesVersion, "ZBZV003", "Rules version is required.", errors);
        Require(ruleSet.Profile, "ZBZV004", "Profile is required.", errors);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < ruleSet.Rules.Count; index++)
        {
            ValidateRule(ruleSet.Rules[index], index, ids, errors);
        }

        var helpIds = new HashSet<string>(
            ruleSet.Help.Select(help => help.DiagnosticId),
            StringComparer.Ordinal);

        foreach (var rule in ruleSet.Rules)
        {
            if (!helpIds.Contains(rule.Id))
            {
                errors.Add(new RuleSetValidationError("ZBZV020", $"Rule '{rule.Id}' must have matching diagnostic help."));
            }
        }

        return errors.Count == 0
            ? RuleSetValidationResult.Success
            : new RuleSetValidationResult(errors);
    }

    private static void ValidateRule(
        RuleDefinition rule,
        int index,
        HashSet<string> ids,
        ICollection<RuleSetValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(rule.Id))
        {
            errors.Add(new RuleSetValidationError("ZBZV010", $"Rule at index {index} must define an id."));
        }
        else
        {
            if (!rule.Id.StartsWith("ZBZ", StringComparison.Ordinal))
            {
                errors.Add(new RuleSetValidationError("ZBZV011", $"Rule '{rule.Id}' must use the ZBZ diagnostic prefix."));
            }

            if (!ids.Add(rule.Id))
            {
                errors.Add(new RuleSetValidationError("ZBZV012", $"Rule '{rule.Id}' is duplicated."));
            }
        }

        Require(rule.Title, "ZBZV013", $"Rule '{rule.Id}' must define a title.", errors);
        Require(rule.Message, "ZBZV014", $"Rule '{rule.Id}' must define a diagnostic message.", errors);

        if (rule.Type == ZerberuzRuleType.Unknown)
        {
            errors.Add(new RuleSetValidationError("ZBZV015", $"Rule '{rule.Id}' must define a supported type."));
        }

        ValidateRegex(rule.Id, "condition.mustMatch", rule.Condition.MustMatch, errors);
        ValidateRegex(rule.Id, "condition.mustNotMatch", rule.Condition.MustNotMatch, errors);
    }

    private static void ValidateRegex(
        string ruleId,
        string property,
        string? pattern,
        ICollection<RuleSetValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException exception)
        {
            errors.Add(new RuleSetValidationError(
                "ZBZV016",
                $"Rule '{ruleId}' has invalid regex in {property}: {exception.Message}"));
        }
    }

    private static void Require(
        string? value,
        string code,
        string message,
        ICollection<RuleSetValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new RuleSetValidationError(code, message));
        }
    }
}
