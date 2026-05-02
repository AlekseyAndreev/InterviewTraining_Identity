using Microsoft.AspNetCore.Mvc;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.WebUI.Extensions;
using SevSharks.Identity.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace SevSharks.Identity.WebUI.Controllers;

public partial class AccountController
{
    ///<summary>
    /// Допустимые роли для регистрации
    ///</summary>
    private static readonly HashSet<string> AllowedRoles = new HashSet<string> { RolesConstants.Candidate, RolesConstants.Expert };

    ///<summary>
    /// Show register page
    ///</summary>
    [HttpGet]
    public IActionResult Register(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) && TempData["ReturnUrl"] != null && !string.IsNullOrEmpty(TempData["ReturnUrl"].ToString()))
        {
            returnUrl = TempData["ReturnUrl"].ToString();
        }

        if (!AllowRegister)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        var registerViewModel = new RegisterViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(registerViewModel);
    }

    ///<summary>
    /// Post register page
    ///</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
    {
        var validCaptcha = await ValidateCaptcha();

        // Валидация ролей
        if (registerViewModel.Roles != null && registerViewModel.Roles.Any())
        {
            var invalidRoles = registerViewModel.Roles.Where(r => !AllowedRoles.Contains(r)).ToList();
            if (invalidRoles.Any())
            {
                ModelState.AddModelError("Roles", $"Недопустимые роли: {string.Join(", ", invalidRoles)}");
            }
        }

        if (ModelState.IsValid && validCaptcha)
        {
            registerViewModel.IsSucceed = true;
            registerViewModel.ErrorMessages = new List<string>();

            var userAndError = await CreateUser(
                registerViewModel.Login,
                registerViewModel.Password,
                registerViewModel.Phone,
                registerViewModel.Roles?.ToArray());
            var user = userAndError.Item1;
            var error = userAndError.Item2;
            if (!string.IsNullOrEmpty(error))
            {
                registerViewModel.IsSucceed = false;
                registerViewModel.ErrorMessages.Add(error);
                return View(registerViewModel);
            }
            if (user == null)
            {
                registerViewModel.IsSucceed = false;
                registerViewModel.ErrorMessages.Add("Ошибка при создании пользователя. Обратитесь к системному администратору");
                return View(registerViewModel);
            }

            if (registerViewModel.IsSucceed && !registerViewModel.ErrorMessages.Any())
            {
                await NotifyUserChangedAsync(user.Id, registerViewModel.Roles);

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // Очищаем returnUrl от параметров IdentityServer (redirect_to_register, redirect_to_login_after_email_confirmation и т.д.)
                var cleanReturnUrl = CleanReturnUrlForRegister(registerViewModel.ReturnUrl);
                var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, cleanReturnUrl);
                await _emailSender.SendEmailConfirmationAsync(registerViewModel.Login, callbackUrl);

                // Добавляем параметр для перенаправления на Login с сообщением о подтверждении email
                var loginReturnUrl = BuildReturnUrlWithEmailConfirmation(registerViewModel.ReturnUrl);
                return RedirectToAction("Login", new { returnUrl = loginReturnUrl });
            }
        }
        else
        {
            registerViewModel.IsSucceed = false;
            registerViewModel.ErrorMessages = new List<string>();
            foreach (var kvp in ModelState)
            {
                var message = kvp.Value.Errors.FirstOrDefault()?.ErrorMessage;
                if (!string.IsNullOrEmpty(message))
                {
                    registerViewModel.ErrorMessages.Add(message);
                }
            }
            if (!validCaptcha)
            {
                registerViewModel.ErrorMessages.Add("Докажите, что Вы не робот");
            }
            if (string.IsNullOrEmpty(registerViewModel.ReturnUrl))
            {
                registerViewModel.ErrorMessages.Add("Регистрация напрямую запрещена");
            }
        }
        return View(registerViewModel);
    }

    ///<summary>
    /// Builds a return URL with email confirmation flag
    ///</summary>
    private string BuildReturnUrlWithEmailConfirmation(string originalReturnUrl)
    {
        if (string.IsNullOrEmpty(originalReturnUrl))
        {
            return null;
        }

        var separator = originalReturnUrl.Contains('?') ? "&" : "?";
        return $"{originalReturnUrl}{separator}redirect_to_login_after_email_confirmation=true";
    }

    ///<summary>
    /// Cleans returnUrl from IdentityServer-specific parameters
    ///</summary>
    private string CleanReturnUrlForRegister(string returnUrl)
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
