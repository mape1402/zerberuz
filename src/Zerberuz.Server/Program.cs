using Zerberuz.Server.Profiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProfileRuleStore, InMemoryProfileRuleStore>();
builder.Services.AddSingleton<RuleProfileResponseFactory>();

var app = builder.Build();

app.MapGet("/", () => "Zerberuz Server shell");
app.MapRuleProfileEndpoints();
app.MapDiagnosticHelpEndpoints();
app.MapRuleValidationEndpoints();

app.Run();

public partial class Program;
