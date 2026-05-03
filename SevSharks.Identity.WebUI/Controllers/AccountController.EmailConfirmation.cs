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

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            TempData[TempDataNameRedirectToLoginAfterConfirmation] = true;
            return RedirectToAction("Login", new { returnUrl = decodedReturnUrl });
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
        var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, model.ReturnUrl);
        await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

        TempData["EmailConfirmationMessage"] = "Письмо для подтверждения email отправлено повторно. Пожалуйста, проверьте почту.";
        return RedirectToAction("Login", "Account");
    }
}
