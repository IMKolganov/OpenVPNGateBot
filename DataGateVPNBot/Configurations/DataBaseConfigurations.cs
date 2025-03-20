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
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                               ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database connection string is missing.");

        var dbSettings = new DataBaseSettings
        {
            DefaultSchema = Environment.GetEnvironmentVariable("DB_DEFAULT_SCHEMA") 
                            ?? configuration["DataBaseSettings:DefaultSchema"],

            MigrationTable = Environment.GetEnvironmentVariable("DB_MIGRATION_TABLE") 
                             ?? configuration["DataBaseSettings:MigrationTable"]
        };

        services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                connectionString,
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