using System;

namespace SevSharks.Identity.WebUI.Configurations;

public static class EnvironmentConstants
{
    public static string VkontakteClientId = Environment.GetEnvironmentVariable("VK_CLIENT_ID");
    public static string VkontakteSecret = Environment.GetEnvironmentVariable("VK_SECRET_KEY");
    public static string GoogleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    public static string GoogleSecret = Environment.GetEnvironmentVariable("GOOGLE_SECRET_KEY");
    public static string MicrosoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
    public static string MicrosoftSecret = Environment.GetEnvironmentVariable("MICROSOFT_SECRET_KEY");
}
