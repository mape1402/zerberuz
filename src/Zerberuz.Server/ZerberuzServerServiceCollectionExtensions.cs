using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zerberuz.Server.Data;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server;

public static class ZerberuzServerServiceCollectionExtensions
{
    public static IServiceCollection AddZerberuzServer(
        this IServiceCollection services,
        Action<ZerberuzServerOptions>? configure = null)
    {
        var options = new ZerberuzServerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.ConfigureHttpJsonOptions(jsonOptions =>
        {
            jsonOptions.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        });

        services.AddDbContext<ZerberuzDbContext>(dbContextOptions =>
        {
            if (options.ConfigureDbContext is null)
            {
                var databasePath = Path.Combine(AppContext.BaseDirectory, "zerberuz.db");
                dbContextOptions.UseSqlite($"Data Source={databasePath}");
                return;
            }

            options.ConfigureDbContext(dbContextOptions);
        });

        services.AddScoped<IProfileRuleStore, EfProfileRuleStore>();
        services.AddSingleton<RuleProfileResponseFactory>();
        services.AddRazorPages()
            .AddApplicationPart(typeof(ZerberuzServerServiceCollectionExtensions).Assembly);

        return services;
    }
}
