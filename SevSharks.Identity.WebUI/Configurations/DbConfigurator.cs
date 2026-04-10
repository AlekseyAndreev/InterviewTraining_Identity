using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevSharks.Identity.DataAccess;

namespace SevSharks.Identity.WebUI.Configurations
{
    public static class DbConfigurator
    {
        public static IServiceCollection AddDb(this IServiceCollection services, IConfiguration configuration)
        {
            void DbContextOptionsBuilder(DbContextOptionsBuilder builder) =>
                builder.UseNpgsql(ConfigurationHelper.GetConnectionStringFromConfig(configuration),
                    b => b.MigrationsAssembly(SevSharksIdentityConfigurationConstants.MigrationAssemblyName));

            services.AddDbContext<Context>(DbContextOptionsBuilder);
            services.AddDbContext<ConfigurationDbContext>(DbContextOptionsBuilder);
            services.AddDbContext<PersistedGrantDbContext>(DbContextOptionsBuilder);
            services.AddDbContext<AppConfigurationDbContext>(DbContextOptionsBuilder);
            services.AddDbContext<AppPersistedGrantDbContext>(DbContextOptionsBuilder);
            return services;
        }
    }
}
