using Microsoft.EntityFrameworkCore;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Data;

namespace Zerberuz.Server.Profiles;

public sealed class EfProfileRuleStore : IProfileRuleStore
{
    private readonly ZerberuzDbContext dbContext;

    public EfProfileRuleStore(ZerberuzDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetVersionsAsync(
        string profile,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RuleProfiles
            .AsNoTracking()
            .Where(candidate => candidate.Profile == profile)
            .OrderByDescending(candidate => candidate.RulesVersion)
            .Select(candidate => candidate.RulesVersion)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RuleSetDefinition?> GetRuleSetAsync(
        string profile,
        string version,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RuleProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Profile == profile && candidate.RulesVersion == version,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? null
            : RuleSetJsonSerializer.Deserialize(entity.RuleSetJson);
    }

    public async Task<RuleSetDefinition?> GetLatestCompatibleRuleSetAsync(
        string profile,
        string engineVersion,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RuleProfiles
            .AsNoTracking()
            .Where(candidate => candidate.Profile == profile)
            .OrderByDescending(candidate => candidate.RulesVersion)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? null
            : RuleSetJsonSerializer.Deserialize(entity.RuleSetJson);
    }

    public async Task<DiagnosticHelpDefinition?> FindHelpAsync(
        string diagnosticId,
        CancellationToken cancellationToken = default)
    {
        var ruleSetJsonValues = await dbContext.RuleProfiles
            .AsNoTracking()
            .Select(profile => profile.RuleSetJson)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ruleSetJsonValues
            .Select(RuleSetJsonSerializer.Deserialize)
            .Where(ruleSet => ruleSet is not null)
            .SelectMany(ruleSet => ruleSet!.Help)
            .FirstOrDefault(help => string.Equals(help.DiagnosticId, diagnosticId, StringComparison.Ordinal));
    }

    public async Task<RuleProfilePublishResult> PublishRuleSetAsync(
        RuleSetDefinition ruleSet,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.RuleProfiles
            .AnyAsync(
                candidate => candidate.Profile == ruleSet.Profile &&
                    candidate.RulesVersion == ruleSet.RulesVersion,
                cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return RuleProfilePublishResult.Conflict(
                "ZBZP001",
                $"Rule profile '{ruleSet.Profile}@{ruleSet.RulesVersion}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.RuleProfiles.Add(new RuleProfileEntity
        {
            Profile = ruleSet.Profile,
            RulesVersion = ruleSet.RulesVersion,
            SchemaVersion = ruleSet.SchemaVersion,
            MinimumEngineVersion = ruleSet.MinimumEngineVersion,
            RuleSetJson = RuleSetJsonSerializer.Serialize(ruleSet),
            CreatedAtUtc = now,
            PublishedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return RuleProfilePublishResult.Success(ruleSet);
    }
}
