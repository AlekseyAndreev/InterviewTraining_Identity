using Microsoft.Extensions.Configuration;
using System;

namespace SevSharks.Identity.WebUI.Configurations
{
    public static class ConfigurationHelper
    {
        public static string GetConnectionStringFromConfig(IConfiguration configuration)
        {
            return GetSettingFromConfig(configuration, "ConnectionStrings", "DefaultConnection");
        }

        public static string GetSettingFromConfig(IConfiguration configuration, string firstName, string secondName)
        {
            string result = configuration[firstName + ":" + secondName];
            if (string.IsNullOrEmpty(result))
            {
                result = configuration[firstName + "_" + secondName];
            }

            if (string.IsNullOrEmpty(result))
            {
                throw new Exception(
                    "Configuration setting does not exist. Setting name " + firstName + ":" + secondName);
            }

            return result;
        }
    }
}
