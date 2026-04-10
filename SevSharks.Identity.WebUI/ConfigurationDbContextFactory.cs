using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.DbContexts;

namespace SevSharks.Identity.WebUI;

public class ConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
{
    public ConfigurationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        var migrationAssembly = "SevSharks.Identity.DataAccess";
        var connectionString = "ConnectionStrings_DefaultConnection: \"Host=localhost;Port=5432;Database=sevsharks_auth;UserId=postgres;Password=password\"";
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationAssembly));

        var result = new ConfigurationDbContext(optionsBuilder.Options);
        result.StoreOptions = new Duende.IdentityServer.EntityFramework.Options.ConfigurationStoreOptions();
        return result;
    }
}
