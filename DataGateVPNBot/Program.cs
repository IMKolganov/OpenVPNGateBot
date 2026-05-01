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
builder.Services.ConfigureServices(builder.Configuration);
builder.Services.ConfigureDashboardApi();

builder.ConfigureWebHost(logger);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});


var app = builder.Build();

app.ConfigureMiddleware();
app.ConfigurePipeline();

app.Run();