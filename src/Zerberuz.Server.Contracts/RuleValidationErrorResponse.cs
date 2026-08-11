namespace Zerberuz.Server.Contracts;

public sealed class RuleValidationErrorResponse
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
