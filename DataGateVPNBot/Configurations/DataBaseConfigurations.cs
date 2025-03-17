using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.Repositories;
using DataGateVPNBot.DataBase.Repositories.Interfaces;
using DataGateVPNBot.DataBase.Repositories.Queries;
using DataGateVPNBot.DataBase.UnitOfWork;
using DataGateVPNBot.Models.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DataGateVPNBot.Configurations;

public static class DataBaseConfigurations
{
    public static void DataBaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbSettings = configuration.GetSection("DataBaseSettings").Get<DataBaseSettings>() 
                         ?? throw new InvalidOperationException("DataBaseSettings section is missing in configuration.");

        services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found."),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    dbSettings.MigrationTable ?? "__EFMigrationsHistory",
                    dbSettings.DefaultSchema ?? "public"
                )
            );
        });
        
        services.AddScoped<IRepositoryFactory, RepositoryFactory>();
        services.AddScoped<IQueryFactory, QueryFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
    }
}