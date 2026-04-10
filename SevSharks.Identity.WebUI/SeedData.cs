using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using SevSharks.Identity.DataAccess;
using SevSharks.Identity.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.BusinessLogic.Models;
using Duende.IdentityServer;
using Client = Duende.IdentityServer.Models.Client;
using IdentityResource = Duende.IdentityServer.Models.IdentityResource;
using ApiResource = Duende.IdentityServer.Models.ApiResource;
using ApiScope = Duende.IdentityServer.Models.ApiScope;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SevSharks.Identity.WebUI;

/// <summary>
/// </summary>
public class SeedData
{
    /// <summary>
    ///     scopes define the resources in your system
    /// </summary>
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new CustomIdentityResources.Roles(),
            new CustomIdentityResources.Permissions()
        ];
    }

    /// <summary>
    ///     GetApiResources
    /// </summary>
    public static IEnumerable<ApiResource> GetApiResources()
    {
        return
        [
            new CustomApiResources.SignalRApiResource(),
            new CustomApiResources.InterviewApiResource()
        ];
    }

    /// <summary>
    ///     GetApiResources
    /// </summary>
    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return
        [
            new ApiScope(name: CustomApiResources.CustomScopes.SignalrScopeName,   displayName: CustomApiResources.CustomScopes.SignalrScopeName),
            new ApiScope(name: CustomApiResources.CustomScopes.InterviewScopeName,   displayName: CustomApiResources.CustomScopes.InterviewScopeName),
        ];
    }

    /// <summary>
    ///     clients want to access resources (aka scopes)
    /// </summary>
    public static IEnumerable<Client> GetClients()
    {
        const int accessTokenLifeTime = 7200;
        const int identityTokenLifeTime = 7200;

        // client credentials client
        return new List<Client>
        {
            // JavaScript Client - Authorization Code Flow with PKCE
            new Client
            {
                ClientId = Constants.WebSpaClientId,
                ClientName = Constants.WebSpaClientName,
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false, // SPA doesn't need client secret when using PKCE
                RequirePkce = true,
                AllowAccessTokensViaBrowser = true,
                RequireConsent = false,

                RedirectUris =
                {
                    "http://localhost:4200/callback",
                    "http://localhost:4200/login/callback",
                    "http://localhost:4200/assets/silent-renew.html",
                    "https://localhost:4200/callback",
                    "https://localhost:4200/login/callback",
                    "https://localhost:4200/assets/silent-renew.html",
                    "http://localhost:5008/callback",
                    "http://localhost:5008/login/callback",
                    "http://localhost:5008/assets/silent-renew.html",
                    "http://webspa/callback",
                    "http://webspa/login/callback",
                    "http://webspa/assets/silent-renew.html",
                    "http://togetherbytaxi.ru:5008/callback",
                    "http://togetherbytaxi.ru:5008/login/callback",
                    "http://togetherbytaxi.ru:5008/assets/silent-renew.html",
                    "http://togetherbytaxi.ru/callback",
                    "http://togetherbytaxi.ru/login/callback",
                    "http://togetherbytaxi.ru/assets/silent-renew.html",
                    "https://togetherbytaxi.ru/callback",
                    "https://togetherbytaxi.ru/login/callback",
                    "https://togetherbytaxi.ru/assets/silent-renew.html",
                },
                PostLogoutRedirectUris = {"http://localhost:4200", "https://localhost:4200", "http://localhost:5008", "http://webspa", "http://togetherbytaxi.ru:5008", "http://togetherbytaxi.ru", "https://togetherbytaxi.ru", },
                AllowedCorsOrigins = {"http://localhost:4200", "https://localhost:4200", "http://localhost:5008", "http://webspa", "http://togetherbytaxi.ru:5008", "http://togetherbytaxi.ru", "https://togetherbytaxi.ru", },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    CustomIdentityResources.CustomScopes.Roles,
                    CustomIdentityResources.CustomScopes.Permissions,
                    CustomApiResources.CustomScopes.SignalrScopeName,
                    CustomApiResources.CustomScopes.InterviewScopeName
                },

                AccessTokenLifetime = accessTokenLifeTime,
                IdentityTokenLifetime = identityTokenLifeTime
            },
            // Client for test
            new Client
            {
                ClientId = Constants.ClientTestId,
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowOfflineAccess = true,
                Enabled = true,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    CustomIdentityResources.CustomScopes.Roles,
                    CustomIdentityResources.CustomScopes.Permissions,
                    CustomApiResources.CustomScopes.SignalrScopeName,
                    CustomApiResources.CustomScopes.InterviewScopeName
                },

                AccessTokenLifetime = accessTokenLifeTime,
                IdentityTokenLifetime = identityTokenLifeTime
            }
        };
    }

    /// <summary>
    ///     EnsureSeedData
    /// </summary>
    public static async Task EnsureSeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedData");
        logger.LogInformation("Start migrate for PersistedGrantDbContext");
        scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
        logger.LogInformation("End migrate for PersistedGrantDbContext");

        var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        logger.LogInformation("Start migrate for ConfigurationDbContext");
        configurationDbContext.Database.Migrate();
        logger.LogInformation("End migrate for ConfigurationDbContext");

        var anyClients = configurationDbContext.Clients.Any();
        logger.LogInformation("ConfigurationDbContext has Clients = {clientsExist}", anyClients);
        if (!anyClients)
        {
            logger.LogInformation("ConfigurationDbContext does not have Clients. Adding clients");
            foreach (var client in GetClients())
            {
                logger.LogInformation("ConfigurationDbContext add Client. {client}", client);
                var clientToEntity = client.ToEntity();
                configurationDbContext.Clients.Add(clientToEntity);
            }

            await configurationDbContext.SaveChangesAsync();
        }

        if (!configurationDbContext.IdentityResources.Any())
        {
            foreach (var resource in GetIdentityResources())
            {
                configurationDbContext.IdentityResources.Add(resource.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }

        if (!configurationDbContext.ApiResources.Any())
        {
            foreach (var resource in GetApiResources())
            {
                configurationDbContext.ApiResources.Add(resource.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }


        if (!configurationDbContext.ApiScopes.Any())
        {
            foreach (var apiScope in GetApiScopes())
            {
                configurationDbContext.ApiScopes.Add(apiScope.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }

        var context = scope.ServiceProvider.GetService<Context>();
        context.Database.Migrate();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
        await SeedRolesAsync(roleManager, logger);

        var createUserService = scope.ServiceProvider.GetService<CreateUserService>();

        await AddAllUsersAsync(createUserService, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            Constants.Roles.Candidate,
            Constants.Roles.Expert,
            Constants.Roles.Admin
        };

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                logger.LogInformation("Creating role: {RoleName}", roleName);
                var role = new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Role {RoleName} created successfully", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }
    }

    private static async Task AddAllUsersAsync(CreateUserService createUserService, ILogger logger)
    {
        try
        {
            logger.LogInformation("Adding all users");
            await CreateUserCandidateAsync(createUserService, logger, "candidate1@mail.ru");
            await CreateUserCandidateAsync(createUserService, logger, "candidate2@mail.ru");
            await CreateUserExpertAsync(createUserService, logger, "expert1@mail.ru");
            await CreateUserExpertAsync(createUserService, logger, "expert2@mail.ru");
            await CreateUserCandidateAndExpertAsync(createUserService, logger, "candidateandexpert1@mail.ru");
            await CreateUserCandidateAndExpertAsync(createUserService, logger, "candidateandexpert2@mail.ru");
            await CreateUserAdminAsync(createUserService, logger, "admin1@mail.ru");
            await CreateUserAdminAsync(createUserService, logger, "admin2@mail.ru");
        }
        catch (Exception e)
        {
            logger.LogError(e ,"Error during adding users");
        }
    }

    private static async Task CreateUserCandidateAsync(CreateUserService createUserService, ILogger logger, string userName) =>
        await CreateUserAsync(createUserService, logger, userName, [ Constants.Roles.Candidate ]);

    private static async Task CreateUserExpertAsync(CreateUserService createUserService, ILogger logger, string userName) =>
        await CreateUserAsync(createUserService, logger, userName, [ Constants.Roles.Expert ]);

    private static async Task CreateUserCandidateAndExpertAsync(CreateUserService createUserService, ILogger logger, string userName) =>
        await CreateUserAsync(createUserService, logger, userName, [ Constants.Roles.Candidate, Constants.Roles.Candidate]);

    private static async Task CreateUserAdminAsync(CreateUserService createUserService, ILogger logger, string userName) =>
        await CreateUserAsync(createUserService, logger, userName, [ Constants.Roles.Admin ]);

    private static async Task CreateUserAsync(CreateUserService createUserService, ILogger logger, string userName, string[] roles)
    {
        logger.LogInformation("Add user {userName}", userName);
        var userDto = new CreateUserDto
        {
            UserName = userName,
            EmailConfirmed = true,
            PhoneNumber = "+79780256699",
            PhoneNumberConfirmed = true,
            Password = "Pass123$",
            Email = userName,
            Roles = roles
        };

        var addedUser = await createUserService.CreateUser(userDto);
        logger.LogInformation("Added user {userName}", addedUser.UserName);
    }
}
