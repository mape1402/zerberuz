using Microsoft.EntityFrameworkCore;

namespace Zerberuz.Server;

public sealed class ZerberuzServerOptions
{
    public bool SeedDefaultProfiles { get; set; } = true;

    internal Action<DbContextOptionsBuilder>? ConfigureDbContext { get; private set; }

    public ZerberuzServerOptions UseSqlite(string connectionString)
    {
        ConfigureDbContext = options => options.UseSqlite(connectionString);
        return this;
    }

    public ZerberuzServerOptions UseDbContext(Action<DbContextOptionsBuilder> configureDbContext)
    {
        ConfigureDbContext = configureDbContext ?? throw new ArgumentNullException(nameof(configureDbContext));
        return this;
    }
}
