using DataGateVPNBot.Configurations;

var builder = WebApplication.CreateBuilder(args);
Console.OutputEncoding = System.Text.Encoding.UTF8;
builder.Host.ConfigureSerilog();

builder.Services.ConfigureTelegram(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.DataBaseServices(builder.Configuration);
builder.Services.ConfigureDashboardApi(builder.Configuration);

builder.ConfigureWebHost();

var app = builder.Build();

app.ConfigureMiddleware();
app.ConfigurePipeline();

app.Run();