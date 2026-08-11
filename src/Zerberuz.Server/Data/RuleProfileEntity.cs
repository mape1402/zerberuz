namespace Zerberuz.Server.Data;

public sealed class RuleProfileEntity
{
    public long Id { get; set; }

    public string Profile { get; set; } = string.Empty;

    public string RulesVersion { get; set; } = string.Empty;

    public string SchemaVersion { get; set; } = string.Empty;

    public string MinimumEngineVersion { get; set; } = string.Empty;

    public string RuleSetJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
