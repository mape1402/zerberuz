namespace Zerberuz.Analyzers.Rules;

public sealed class RuleSetValidationError
{
    public RuleSetValidationError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}
