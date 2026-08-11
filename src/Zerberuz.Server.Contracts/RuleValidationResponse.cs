namespace Zerberuz.Server.Contracts;

public sealed class RuleValidationResponse
{
    public bool IsValid { get; set; }

    public IList<RuleValidationErrorResponse> Errors { get; set; } = new List<RuleValidationErrorResponse>();
}
