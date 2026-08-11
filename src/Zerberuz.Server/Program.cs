using Zerberuz.Server;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.Combine(AppContext.BaseDirectory, "zerberuz.db");
var connectionString = builder.Configuration.GetConnectionString("Zerberuz") ?? $"Data Source={databasePath}";

builder.Services.AddZerberuzServer(options => options.UseSqlite(connectionString));

var app = builder.Build();
await app.Services.InitializeZerberuzServerAsync();

app.MapGet("/", () => "Zerberuz Server shell");
app.MapZerberuzServer();

app.Run();

public partial class Program;
