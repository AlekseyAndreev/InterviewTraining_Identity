using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SevSharks.Identity.WebUI.ExternalAuthentication.VkConnection;
using System.Security.Claims;

namespace SevSharks.Identity.WebUI.Configurations;

public static class AuthenticationConfigurator
{
    public static IServiceCollection AddSevSharksAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddVk("VK", options =>
            {
                options.ApiVersion = "8.57";
                options.ClientId = EnvironmentConstants.VkontakteClientId;
                options.ClientSecret = EnvironmentConstants.VkontakteSecret;
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                options.Scope.Add("email");

                options.Fields.Add("uid");
                options.Fields.Add("first_name");
                options.Fields.Add("last_name");

                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "uid");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
            })
            .AddGoogle("Google", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = EnvironmentConstants.GoogleClientId;
                options.ClientSecret = EnvironmentConstants.GoogleSecret;
            })
            .AddMicrosoftAccount(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = EnvironmentConstants.MicrosoftClientId;
                options.ClientSecret = EnvironmentConstants.MicrosoftSecret;
            });
        return services;
    }
}
