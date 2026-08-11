using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

public sealed class RuleProfilePublishResult
{
    private RuleProfilePublishResult(bool succeeded, RuleSetDefinition? ruleSet, string errorCode, string errorMessage)
    {
        Succeeded = succeeded;
        RuleSet = ruleSet;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public RuleSetDefinition? RuleSet { get; }

    public string ErrorCode { get; }

    public string ErrorMessage { get; }

    public static RuleProfilePublishResult Success(RuleSetDefinition ruleSet)
    {
        return new RuleProfilePublishResult(true, ruleSet, string.Empty, string.Empty);
    }

    public static RuleProfilePublishResult Conflict(string code, string message)
    {
        return new RuleProfilePublishResult(false, null, code, message);
    }
}
