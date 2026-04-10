using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SevSharks.Identity.WebUI;

var host = Startup.CreateHostBuilder(args).Build();

var services = host.Services;

// Seed data
await SeedData.EnsureSeedDataAsync(services);

host.Run();