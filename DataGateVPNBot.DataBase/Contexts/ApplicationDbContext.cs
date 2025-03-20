using DataGateVPNBot.DataBase.ConfigurationModels;
using DataGateVPNBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataGateVPNBot.DataBase.Contexts;

public class ApplicationDbContext : DbContext
{
    private readonly string _defaultSchema;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _defaultSchema = (Environment.GetEnvironmentVariable("DB_DEFAULT_SCHEMA") 
                          ?? configuration["DataBaseSettings:DefaultSchema"]) ?? "public";
    }
    
    public DbSet<TelegramUser> TelegramUsers { get; set; } = null!;
    public DbSet<IssuedOvpnFile> IssuedOvpnFiles { get; set; } = null!;
    public DbSet<UserLanguagePreference> UserLanguagePreferences { get; set; } = null!;
    public DbSet<LocalizationText> LocalizationTexts { get; set; } = null!;
    public DbSet<IncomingMessageLog> IncomingMessageLog { get; set; } = null!;
    public DbSet<OpenVpnUserStatistic> OpenVpnUserStatistics { get; set; } = null!;
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_defaultSchema);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TelegramUserConfiguration());
        modelBuilder.ApplyConfiguration(new IssuedOvpnFileConfiguration());
        modelBuilder.ApplyConfiguration(new UserLanguagePreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new LocalizationTextConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnUserStatisticConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorLogConfiguration());
    }
    

}
