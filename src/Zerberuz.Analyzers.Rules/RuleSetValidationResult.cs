namespace Zerberuz.Analyzers.Rules;

public sealed class RuleSetValidationResult
{
    public RuleSetValidationResult(IReadOnlyCollection<RuleSetValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyCollection<RuleSetValidationError> Errors { get; }

    public static RuleSetValidationResult Success { get; } = new(Array.Empty<RuleSetValidationError>());
}
