using Microsoft.AspNetCore.Mvc;
using SevSharks.Identity.WebUI.Extensions;
using SevSharks.Identity.WebUI.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace SevSharks.Identity.WebUI.Controllers;

public partial class AccountController
{
    ///<summary>
    /// Подтверждение email по ссылке из письма
    ///</summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string code, string returnUrl = null)
    {
        if (userId == null || code == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Не удалось найти пользователя с ID '{userId}'.");
        }

        // Декодируем returnUrl если он был закодирован
        string decodedReturnUrl = null;
        if (!string.IsNullOrEmpty(returnUrl))
        {
            try
            {
                decodedReturnUrl = Encoding.UTF8.GetString(Convert.FromBase64String(returnUrl));
            }
            catch
            {
                // Если декодирование не удалось, используем как есть
                decodedReturnUrl = returnUrl;
            }
        }

        // Очищаем returnUrl от параметров IdentityServer
        var cleanReturnUrl = CleanReturnUrl(decodedReturnUrl);

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            TempData["EmailConfirmationMessage"] = "Ваш email успешно подтверждён. Теперь вы можете войти в систему.";
            // Формируем URL для возврата с флагом подтверждения email
            var loginUrl = BuildLoginUrlWithConfirmation(cleanReturnUrl);
            return Redirect(loginUrl);
        }

        TempData["EmailConfirmationMessage"] = "Ошибка при подтверждении email. Пожалуйста, попробуйте ещё раз.";
        return RedirectToAction("Login", "Account");
    }

    ///<summary>
    /// Страница с информацией о необходимости подтвердить email и кнопкой повторной отправки
    ///</summary>
    [HttpGet]
    public IActionResult EmailConfirmationRequired(string email, string returnUrl)
    {
        var model = new EmailConfirmationRequiredViewModel
        {
            Email = email,
            ReturnUrl = returnUrl
        };
        return View(model);
    }

    ///<summary>
    /// Повторная отправка письма для подтверждения email
    ///</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmationEmail(EmailConfirmationRequiredViewModel model)
    {
        if (string.IsNullOrEmpty(model.Email))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Не раскрываем информацию о существовании пользователя
            TempData["EmailConfirmationMessage"] = "Если аккаунт с таким email существует, письмо для подтверждения отправлено повторно.";
            return RedirectToAction("Login", "Account");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            TempData["EmailConfirmationMessage"] = "Ваш email уже подтверждён. Вы можете войти в систему.";
            return RedirectToAction("Login", "Account");
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        // Очищаем returnUrl от параметров IdentityServer
        var cleanReturnUrl = CleanReturnUrl(model.ReturnUrl);
        var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, cleanReturnUrl);
        await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

        TempData["EmailConfirmationMessage"] = "Письмо для подтверждения email отправлено повторно. Пожалуйста, проверьте почту.";
        return RedirectToAction("Login", "Account");
    }

    ///<summary>
    /// Builds a URL for Login page with email confirmation flag and returnUrl
    ///</summary>
    private string BuildLoginUrlWithConfirmation(string returnUrl)
    {
        var loginUrl = Url.Action("Login", "Account");
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var separator = returnUrl.Contains('?') ? "&" : "?";
            returnUrl = Uri.EscapeDataString(returnUrl);
            return $"{loginUrl}?returnUrl={returnUrl}&redirect_to_login_after_email_confirmation=true";
        }
        return $"{loginUrl}?redirect_to_login_after_email_confirmation=true";
    }

    ///<summary>
    /// Cleans returnUrl from IdentityServer-specific parameters
    ///</summary>
    private string CleanReturnUrl(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return null;
        }

        // Параметры IdentityServer, которые не нужны в ссылке подтверждения
        var paramsToRemove = new[]
        {
            "redirect_to_register",
            "redirect_to_login_after_email_confirmation",
            "redirect_to_change_roles"
        };

        try
        {
            var uri = new Uri(returnUrl.Contains("://") ? returnUrl : "http://temp" + returnUrl);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            foreach (var param in paramsToRemove)
            {
                queryParams.Remove(param);
            }

            var baseUrl = uri.GetLeftPart(UriPartial.Path);
            var newQuery = queryParams.ToString();
            return string.IsNullOrEmpty(newQuery) ? baseUrl : $"{baseUrl}?{newQuery}";
        }
        catch
        {
            // Если что-то пошло не так, возвращаем базовый URL без параметров
            var questionMarkIndex = returnUrl.IndexOf('?');
            return questionMarkIndex > 0 ? returnUrl.Substring(0, questionMarkIndex) : returnUrl;
        }
    }
}
