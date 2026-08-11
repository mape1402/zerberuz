using System.Text.Json;
using System.Text.Json.Serialization;
using Zerberuz.Server.Profiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

builder.Services.AddSingleton<IProfileRuleStore, InMemoryProfileRuleStore>();
builder.Services.AddSingleton<RuleProfileResponseFactory>();

var app = builder.Build();

app.MapGet("/", () => "Zerberuz Server shell");
app.MapRuleProfileEndpoints();
app.MapDiagnosticHelpEndpoints();
app.MapRuleValidationEndpoints();

app.Run();

public partial class Program;
