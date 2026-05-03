using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SevSharks.Identity.BusinessLogic.Services;
using SevSharks.Identity.WebUI.Configurations;
using SevSharks.Identity.WebUI.Options;
using SevSharks.Identity.WebUI.Services;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace SevSharks.Identity.WebUI;

///<summary>
/// Startup
///</summary>
public class Startup
{
    ///<summary>
    /// Constructor
    ///</summary>
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    ///<summary>
    /// IConfiguration
    ///</summary>
    public IConfiguration Configuration { get; }

    ///<summary>
    /// IConfiguration
    ///</summary>
    public IWebHostEnvironment Environment { get; }

    ///<summary>
    /// CreateWebHostBuilder
    ///</summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((hostingContext, config) =>
               {
                   config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                   config.AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);
                   config.AddJsonFile("secrets/appsettings.secrets.json", optional: true);
                   config.AddEnvironmentVariables();
                   config.AddCommandLine(args);
               })
                .UseSerilog((hostingContext, loggerConfig) =>
                {
                    loggerConfig.ReadFrom.Configuration(hostingContext.Configuration);
                })
               .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
               .UseDefaultServiceProvider(configure =>
               {
                   configure.ValidateOnBuild = true;
                   configure.ValidateScopes = true;
               });

    ///<summary>
    /// ConfigureServices
    ///</summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddResponseCompression()
            .AddHttpContextAccessor()
            .AddLocalization(options => options.ResourcesPath = "Resources");

        services
            .AddControllersWithViews()
            .AddRazorRuntimeCompilation()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Add session support for TempData
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".SevSharks.Identity.Session";
        });

        // Configure SMTP options
        services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));

        services.AddDb(Configuration);
        services.AddIdentityServerForSevShark(Configuration, Environment);
        services.AddSevSharksAuthentication();

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // HttpClient для webhook с игнорированием HTTPS-сертификата
        services.AddHttpClient(UserSyncWebhookService.WebhookClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });

        // CORS для разрешения запросов от SPA
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpa", policy =>
            {
                var mainUrl = Configuration["MainUrl"] ?? "http://localhost:4200";
                var uri = new Uri(mainUrl);
                policy.WithOrigins($"{uri.Scheme}://{uri.Host}:{uri.Port}")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        services.AddSingleton(Configuration);
        services.AddSingleton((IConfigurationRoot) Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        var forwardedHeaderOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost
        };
        forwardedHeaderOptions.KnownIPNetworks.Clear();
        forwardedHeaderOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardedHeaderOptions);

        var supportedCultures = new[] { "ru", "en" };
        var localizationOptions = new RequestLocalizationOptions()
        {
            ApplyCurrentCultureToResponseHeaders = true
        }
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        app.UseRequestLocalization(localizationOptions);

        app.UseStaticFiles()
            .UseRouting()
            .UseCors("AllowSpa")
            .UseSession()
            .UseResponseCompression()
            .UseAuthentication()
            .UseHttpsRedirection()
            .UseAuthorization()
            .UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute())
            .UseIdentityServer();
    }
}
