using Microsoft.EntityFrameworkCore;

namespace Zerberuz.Server.Data;

public sealed class ZerberuzDbContext : DbContext
{
    public ZerberuzDbContext(DbContextOptions<ZerberuzDbContext> options)
        : base(options)
    {
    }

    public DbSet<RuleProfileEntity> RuleProfiles => Set<RuleProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ruleProfile = modelBuilder.Entity<RuleProfileEntity>();
        ruleProfile.ToTable("rule_profiles");
        ruleProfile.HasKey(profile => profile.Id);
        ruleProfile.Property(profile => profile.Profile).HasMaxLength(160).IsRequired();
        ruleProfile.Property(profile => profile.RulesVersion).HasMaxLength(80).IsRequired();
        ruleProfile.Property(profile => profile.SchemaVersion).HasMaxLength(40).IsRequired();
        ruleProfile.Property(profile => profile.MinimumEngineVersion).HasMaxLength(40).IsRequired();
        ruleProfile.Property(profile => profile.RuleSetJson).IsRequired();
        ruleProfile.HasIndex(profile => profile.Profile);
        ruleProfile.HasIndex(profile => new { profile.Profile, profile.RulesVersion }).IsUnique();
    }
}
