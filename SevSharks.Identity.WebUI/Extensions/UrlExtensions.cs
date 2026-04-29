using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace SevSharks.Identity.WebUI.Extensions;

public static class UrlExtensions
{
    public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
    {
        return urlHelper.Action(new UrlActionContext
        {
            Action = nameof(Controllers.AccountController.ConfirmEmail),
            Controller = "Account",
            Values = new { userId, code },
            Protocol = scheme
        });
    }
}