using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevSharks.Identity.DataAccess.Models;
using SevSharks.Identity.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace SevSharks.Identity.WebUI.Controllers;

public partial class AccountController
{
    /// <summary>
    /// Show ChangeRoles page
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangeRoles(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) && TempData["ReturnUrl"] != null && !string.IsNullOrEmpty(TempData["ReturnUrl"].ToString()))
        {
            returnUrl = TempData["ReturnUrl"].ToString();
        }

        ViewData["ReturnUrl"] = returnUrl;
        var userId = GetCurrentUserId();
        var (user, roles) = await _userService.GetUserInfoById(userId);

        var changeUserRolesViewModel = new ChangeUserRolesViewModel
        {
            ReturnUrl = returnUrl,
            Login = user.UserName,
            Roles = roles,
        };

        return View(changeUserRolesViewModel);
    }

    /// <summary>
    /// Post ChangeRoles page
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRoles(ChangeUserRolesViewModel changeUserRolesViewModel)
    {
        var validCaptcha = await ValidateCaptcha();

        // Валидация ролей
        if (changeUserRolesViewModel.Roles != null && changeUserRolesViewModel.Roles.Any())
        {
            var invalidRoles = changeUserRolesViewModel.Roles.Where(r => !AllowedRoles.Contains(r)).ToList();
            if (invalidRoles.Any())
            {
                ModelState.AddModelError("Roles", $"Недопустимые роли: {string.Join(", ", invalidRoles)}");
            }
        }

        if (ModelState.IsValid && validCaptcha)
        {
            changeUserRolesViewModel.IsSucceed = true;
            changeUserRolesViewModel.ErrorMessages = new List<string>();

            var (user, error) = await ChangeUserRoles(
                changeUserRolesViewModel.Login,
                changeUserRolesViewModel.Roles?.ToArray());
            if (!string.IsNullOrEmpty(error))
            {
                changeUserRolesViewModel.IsSucceed = false;
                changeUserRolesViewModel.ErrorMessages.Add(error);
                return View(changeUserRolesViewModel);
            }
            if (user == null)
            {
                changeUserRolesViewModel.IsSucceed = false;
                changeUserRolesViewModel.ErrorMessages.Add("Ошибка при создании пользователя. Обратитесь к системному администратору");
                return View(changeUserRolesViewModel);
            }

            if (changeUserRolesViewModel.IsSucceed && !changeUserRolesViewModel.ErrorMessages.Any())
            {
                await NotifyUserChangedAsync(user.Id, changeUserRolesViewModel.Roles);

                return await SignInAndRedirect(user, changeUserRolesViewModel.ReturnUrl);
            }
        }
        else
        {
            changeUserRolesViewModel.IsSucceed = false;
            changeUserRolesViewModel.ErrorMessages = new List<string>();
            foreach (var kvp in ModelState)
            {
                var message = kvp.Value.Errors.FirstOrDefault()?.ErrorMessage;
                if (!string.IsNullOrEmpty(message))
                {
                    changeUserRolesViewModel.ErrorMessages.Add(message);
                }
            }
            if (!validCaptcha)
            {
                changeUserRolesViewModel.ErrorMessages.Add("Докажите, что Вы не робот");
            }
            if (string.IsNullOrEmpty(changeUserRolesViewModel.ReturnUrl))
            {
                changeUserRolesViewModel.ErrorMessages.Add("Регистрация напрямую запрещена");
            }
        }
        return View(changeUserRolesViewModel);
    }

    private async Task<(ApplicationUser user, string error)> ChangeUserRoles(string login, string[] roles = null)
    {
        ApplicationUser user = null;
        string error = string.Empty;
        try
        {
            user = await _userService.UpdateUserRoles(login, roles);
        }
        catch (Exception e)
        {
            error = e.Message;
            return (user, error);
        }

        return (user, error);
    }
}
