using DataGateVPNBot.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureTelegram(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.DataBaseServices(builder.Configuration);
builder.Services.ConfigureDashboardApi(builder.Configuration);

builder.Host.ConfigureSerilog(builder.Configuration);

builder.ConfigureWebHost();

var app = builder.Build();

app.ConfigureMiddleware();
app.ConfigurePipeline();

app.Run();