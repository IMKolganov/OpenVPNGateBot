using System.Reflection;
using DataGateVPNBot.Configurations;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
Console.OutputEncoding = System.Text.Encoding.UTF8;
builder.Host.ConfigureSerilog();
var logger = Log.ForContext("SourceContext", "DILogger");
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
logger.Information($"Application version: {version};");

builder.Services.ConfigureTelegram(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureDashboardApi();

builder.ConfigureWebHost(logger);

var app = builder.Build();

app.ConfigureMiddleware();
app.ConfigurePipeline();

app.Run();