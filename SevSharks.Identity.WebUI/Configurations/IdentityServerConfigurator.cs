using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.BusinessLogic.Services;
using SevSharks.Identity.DataAccess.Models;
using SevSharks.Identity.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using System;

namespace SevSharks.Identity.WebUI.Configurations;

public static class IdentityServerConfigurator
{
    public static IServiceCollection AddIdentityServerForSevShark(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var options = new Duende.IdentityServer.EntityFramework.Options.ConfigurationStoreOptions();
        services.AddSingleton(options);

        string connectionString = ConfigurationHelper.GetConnectionStringFromConfig(configuration);

        // Добавляем HttpClientFactory для webhook
        services.AddHttpClient();
        services.AddScoped<IUserSyncWebhookService, UserSyncWebhookService>();

        var identityServerBuilder = services
            .AddIdentityServer();

        // Регистрируем RSA Key Service как singleton
        services.AddSingleton<RsaKeyService>(sp =>
        {
            // Ключи будут храниться 365 дней
            var rsaKeyService = new RsaKeyService(environment, TimeSpan.FromDays(365));
            // При первом запуске генерируем ключ, если его нет
            if (rsaKeyService.NeedsUpdate())
            {
                rsaKeyService.GenerateKeyAndSave();
            }
            return rsaKeyService;
        });

        // Используем RSA ключи из файла вместо DeveloperSigningCredential
        var rsaKeyService = services.BuildServiceProvider().GetRequiredService<RsaKeyService>();
        var rsaSecurityKey = rsaKeyService.GetKey();
        identityServerBuilder.AddSigningCredential(rsaSecurityKey, Duende.IdentityServer.IdentityServerConstants.RsaSigningAlgorithm.RS256);

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
                opt.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<Context>()
            .AddDefaultTokenProviders();

        services.AddTransient<UserManager<ApplicationUser>>();
        services.AddTransient<ExternalSystemAccountService>();
        services.AddTransient<IProfileService, IdentityWithAdditionalClaimsProfileService>();
        services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
        services.AddTransient<IUserClaimsPrincipalFactory<ApplicationUser>,
            UserClaimsFactory<ApplicationUser, ApplicationRole>>();
        services.AddTransient<UserService, UserService>();

        return services;
    }
}
