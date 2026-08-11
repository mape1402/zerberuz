using Microsoft.EntityFrameworkCore;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Data;

public static class ZerberuzDatabaseInitializer
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    public static async Task InitializeAsync(
        IServiceProvider services,
        bool seedDefaultProfiles = true)
    {
        await InitializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ZerberuzDbContext>();

            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            if (seedDefaultProfiles)
            {
                await SeedProfileAsync(dbContext, ProfileSeedData.CreateBackendRuleSet()).ConfigureAwait(false);
            }
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    private static async Task SeedProfileAsync(
        ZerberuzDbContext dbContext,
        Analyzers.Rules.RuleSetDefinition ruleSet)
    {
        var exists = await dbContext.RuleProfiles.AnyAsync(profile =>
                profile.Profile == ruleSet.Profile &&
                profile.RulesVersion == ruleSet.RulesVersion)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
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

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
