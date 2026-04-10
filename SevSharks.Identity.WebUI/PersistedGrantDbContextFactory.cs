using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using SevSharks.Identity.WebUI.Configurations;

namespace SevSharks.Identity.WebUI;

public class PersistedGrantDbContextFactory : IDesignTimeDbContextFactory<PersistedGrantDbContext>
{
    public PersistedGrantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PersistedGrantDbContext>();
        optionsBuilder.UseNpgsql("ConnectionStrings_DefaultConnection: \"Host=localhost;Port=5432;Database=sevsharks_auth;UserId=postgres;Password=password\"", b => b.MigrationsAssembly(SevSharksIdentityConfigurationConstants.MigrationAssemblyName));

        var result = new PersistedGrantDbContext(optionsBuilder.Options);
        result.StoreOptions = new Duende.IdentityServer.EntityFramework.Options.OperationalStoreOptions();
        return result;
    }
}
