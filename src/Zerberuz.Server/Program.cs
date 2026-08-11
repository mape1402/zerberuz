using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Zerberuz.Server.Data;
using Zerberuz.Server.Profiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

var databasePath = Path.Combine(AppContext.BaseDirectory, "zerberuz.db");
var connectionString = builder.Configuration.GetConnectionString("Zerberuz") ?? $"Data Source={databasePath}";

builder.Services.AddDbContext<ZerberuzDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IProfileRuleStore, EfProfileRuleStore>();
builder.Services.AddSingleton<RuleProfileResponseFactory>();

var app = builder.Build();
await ZerberuzDatabaseInitializer.InitializeAsync(app.Services);

app.MapGet("/", () => "Zerberuz Server shell");
app.MapRuleProfileEndpoints();
app.MapDiagnosticHelpEndpoints();
app.MapRuleValidationEndpoints();

app.Run();

public partial class Program;
