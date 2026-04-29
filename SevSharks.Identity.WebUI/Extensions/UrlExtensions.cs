using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace SevSharks.Identity.WebUI.Extensions;

public static class UrlExtensions
{
    public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme, string returnUrl = null)
    {
        // Если есть returnUrl, кодируем его в Base64
        string encodedReturnUrl = null;
        if (!string.IsNullOrEmpty(returnUrl))
        {
            encodedReturnUrl = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(returnUrl));
        }

        return urlHelper.Action(new UrlActionContext
        {
            Action = nameof(Controllers.AccountController.ConfirmEmail),
            Controller = "Account",
            Values = new RouteValueDictionary
            {
                { "userId", userId },
                { "code", code },
                { "returnUrl", encodedReturnUrl }
            },
            Protocol = scheme
        });
    }
}
