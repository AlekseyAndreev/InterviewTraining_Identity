using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.DataAccess.Models;
using SevSharks.Identity.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;

namespace SevSharks.Identity.WebUI.Configurations;

public static class IdentityServerConfigurator
{
    public static IServiceCollection AddIdentityServerForSevShark(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var options = new Duende.IdentityServer.EntityFramework.Options.ConfigurationStoreOptions();
        services.AddSingleton(options);

        string connectionString = ConfigurationHelper.GetConnectionStringFromConfig(configuration);

        var identityServerBuilder = services
            .AddIdentityServer();

        //var cert = new X509Certificate2("certificates/togetherbytaxi.ru.pfx", "12345678");
        //identityServerBuilder.AddSigningCredential(cert);
        // TODO add certificate
        if (environment.IsDevelopment() || environment.IsEnvironment("Local") || true)
        {
            identityServerBuilder.AddDeveloperSigningCredential();
        }
        else
        {
            /*
            string certificateData = configuration["Certificate:Data"];
            string certificatePassword = configuration["Certificate:Password"];
            identityServerBuilder.AddSigningCredential(new X509Certificate2(
                Convert.FromBase64String(certificateData),
                configuration.GetValue<string>(certificatePassword))); 
            */
            /*
            var cert = new X509Certificate2("certificates\\togetherbytaxi.ru.pfx", "12345678");
            identityServerBuilder.AddSigningCredential(cert);
            */
        }

        identityServerBuilder.AddConfigurationStore(option =>
                option.ConfigureDbContext = builder => builder.UseNpgsql(connectionString, options =>
                    options.MigrationsAssembly(SevSharksIdentityConfigurationConstants.MigrationAssemblyName)))
            .AddOperationalStore(option =>
                option.ConfigureDbContext = builder => builder.UseNpgsql(connectionString, options =>
                    options.MigrationsAssembly(SevSharksIdentityConfigurationConstants.MigrationAssemblyName)));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(opt =>
            {
                // Basic built in validations
                opt.Password.RequireDigit = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequiredLength = 6;
                opt.User.AllowedUserNameCharacters = null;
                opt.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<Context>()
            .AddDefaultTokenProviders();

        services.AddTransient<UserManager<ApplicationUser>>();
        services.AddTransient<ExternalSystemAccountService>();
        services.AddTransient<IProfileService, IdentityWithAdditionalClaimsProfileService>();
        services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
        services.AddTransient<IUserClaimsPrincipalFactory<ApplicationUser>,
            UserClaimsFactory<ApplicationUser, ApplicationRole>>();
        services.AddTransient<CreateUserService, CreateUserService>();

        return services;
    }
}
