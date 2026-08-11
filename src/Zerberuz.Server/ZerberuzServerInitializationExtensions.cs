using Microsoft.Extensions.DependencyInjection;
using Zerberuz.Server.Data;

namespace Zerberuz.Server;

public static class ZerberuzServerInitializationExtensions
{
    public static async Task InitializeZerberuzServerAsync(this IServiceProvider services)
    {
        var options = services.GetRequiredService<ZerberuzServerOptions>();
        await ZerberuzDatabaseInitializer.InitializeAsync(
                services,
                options.SeedDefaultProfiles)
            .ConfigureAwait(false);
    }
}
