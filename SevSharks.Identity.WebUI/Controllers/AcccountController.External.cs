using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;
using Microsoft.AspNetCore.Authentication;
using SevSharks.Identity.DataAccess.Models;
using Duende.IdentityServer;
using Duende.IdentityModel;

namespace SevSharks.Identity.WebUI.Controllers;

public partial class AccountController
{
    /// <summary>
    /// Редактирование внешних систем пользователя
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditUserExternalSystemAccount([FromQuery] string returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var currentUserId = GetCurrentUserId();
        var externalSystemInfoViewModel = new ExternalSystemInfoViewModel();

        var externalSystemAccounts = await _externalSystemAccountService.GetUsersExternalSystemAccounts(currentUserId);
        var loginProviders = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!loginProviders.Any())
        {
            _logger.LogError("Отсутствуют логин-провайдеры внешних систем");
            Redirect(returnUrl);
        }

        if (externalSystemAccounts?.Count > 0)
        {
            externalSystemInfoViewModel.UserExternalSystemNames.AddRange(externalSystemAccounts.Select(s => s.ExternalSystemName));
        }

        externalSystemInfoViewModel.RemainExternalSystemNames
            .AddRange(loginProviders
                .Where(s => !externalSystemInfoViewModel.UserExternalSystemNames
                    .Contains(s.Name))
                .Select(s => s.Name));

        return View(externalSystemInfoViewModel);
    }

    /// <summary>
    /// Добавление внешней системы зарегистрированному пользователю
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult AddUserExternalSystemAccount(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(AddUserExternalSystemAccountCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Удаление внешней системы зарегистрированному пользователю
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserExternalSystemAccount(string provider, string returnUrl = null)
    {
        await _externalSystemAccountService.DeleteUsersExternalSystemAccount(GetCurrentUserId(), provider);
        return Redirect(returnUrl);
    }

    /// <summary>
    /// Добавление внешней системы зарегистрированному пользователю
    /// </summary>        
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> AddUserExternalSystemAccountCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        await _externalSystemAccountService.AddUsersExternalSystemAccount(GetCurrentUserId(), info.LoginProvider, info.ProviderKey);
        return Redirect(returnUrl);
    }

    /// <summary>
    /// ExternalLogin
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// ExternalLoginCallback
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        // read external identity from the temporary cookie
        var result = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        if (result?.Succeeded != true)
        {
            ErrorMessage = $"Error from external provider from temporary cookie";
            return RedirectToAction(nameof(Login));
        }

        // lookup our user and external provider info
        var (user, provider, providerUserId, email) = await FindUserFromExternalProviderAsync(result);
        if (user == null)
        {
            // this might be where you might initiate a custom workflow for user registration
            // in this sample we don't show how that would be done, as our sample implementation
            // simply auto-provisions new external user
            user = await CreateNewUserAsync(provider, providerUserId, email);
        }

        /*
        if (result.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }
        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2Fa), new { returnUrl });
        }
        if (result.IsLockedOut)
        {
            return RedirectToAction(nameof(Lockout));
        }
        // If the user does not have an account, then ask the user to create an account.
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;

        var externalSystemIdentifier = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);

        var currentUser = await _userManager.Users
            .Include(s => s.ExternalLogins)
            .FirstOrDefaultAsync(x => x.ExternalLogins
            .Any(s => s.ExternalUserName == externalSystemIdentifier && s.ExternalSystemName == info.LoginProvider));

        if (currentUser == null)
        {
            if (!AllowRegister)
            {
                var loginViewModel = await BuildLoginViewModelAsync(returnUrl);
                loginViewModel.ErrorMessage = $"Аккаунт {info.LoginProvider} не найден";
                return View(nameof(Login), loginViewModel);
            }
            return View("ExternalLogin", new ExternalLoginViewModel { Login = email, ExternalSystemIdentifier = externalSystemIdentifier, ExternalSystemName = info.LoginProvider });
        }
        */
        return await SignInAndRedirect(user, returnUrl);
    }

    /// <summary>
    /// ExternalLoginConfirmation
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null, string loginProvider = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = loginProvider;
        var validCaptcha = await ValidateCaptcha();
        if (ModelState.IsValid && validCaptcha)
        {
            model.IsSucceed = true;
            model.ErrorMessages = new List<string>();
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                throw new ApplicationException("Error loading external login information during confirmation.");
            }
            var userAndError = await CreateUser(model.Login, string.Empty, model.Phone, roles: null, externalSystemIdentifier: model.ExternalSystemIdentifier, externalSystemName: model.ExternalSystemName);
            var user = userAndError.Item1;
            var error = userAndError.Item2;
            if (!string.IsNullOrEmpty(error))
            {
                model.IsSucceed = false;
                model.ErrorMessages.Add(error);
                return View(nameof(ExternalLogin), model);
            }
            if (user == null)
            {
                model.IsSucceed = false;
                model.ErrorMessages.Add("Ошибка при создании пользователя. Обратитесь к системному администратору");
                return View(nameof(ExternalLogin), model);
            }

            if (model.IsSucceed && !model.ErrorMessages.Any())
            {
                //TODO: GenerateEmailConfirmationTokenAsync
                //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                return await SignInAndRedirect(user, returnUrl);
            }
        }

        return View(nameof(ExternalLogin), model);
    }

    private async Task<(ApplicationUser user, string provider, string providerUserId, string email)> FindUserFromExternalProviderAsync(AuthenticateResult result)
    {
        var externalUser = result.Principal;

        // try to determine the unique id of the external user (issued by the provider)
        // the most common claim type for that are the sub claim and the NameIdentifier
        // depending on the external provider, some other claim type might be used
        var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                          externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                          throw new Exception("Unknown userid");

        var emailClaim = externalUser.FindFirst(JwtClaimTypes.Email) ??
                          externalUser.FindFirst(ClaimTypes.Email);
        var email = emailClaim?.Value;
        // remove the user id claim so we don't include it as an extra claim if/when we provision the user
        var claims = externalUser.Claims.ToList();
        claims.Remove(userIdClaim);

        var provider = GetProvider();
        var providerUserId = userIdClaim.Value;
        ApplicationUser currentUser = null;
        if (!string.IsNullOrEmpty(email))
        {
            currentUser = await _userManager.Users
                .Include(x => x.ExternalLogins)
                .Where(x => x.Email == email).FirstOrDefaultAsync();
            if (currentUser != null && (currentUser.ExternalLogins == null || !currentUser.ExternalLogins.Any() || !currentUser.ExternalLogins.Where(x => x.ExternalSystemName == provider && x.ExternalUserName == providerUserId).Any()))
            {
                if (currentUser.ExternalLogins == null)
                {
                    currentUser.ExternalLogins = new List<UserExternalLogin>();
                }
                currentUser.ExternalLogins.Add(new UserExternalLogin
                {
                    ExternalSystemName = provider,
                    ExternalUserName = providerUserId
                });
                var resultUpdateUsed = await _userManager.UpdateAsync(currentUser);
                if (!resultUpdateUsed.Succeeded)
                {
                    throw new Exception(resultUpdateUsed.Errors.First().Description);
                }
            }
        }

        if (currentUser == null)
        {
            currentUser = await _userManager.Users
                                .Include(s => s.ExternalLogins)
                                .FirstOrDefaultAsync(x => x.ExternalLogins
                                .Any(s => s.ExternalUserName == providerUserId && s.ExternalSystemName == provider));
        }

        return (currentUser, provider, providerUserId, email);

        string GetProvider()
        {
            var items = result.Properties.Items;
            if (items.ContainsKey(".AuthScheme"))
            {
                return items[".AuthScheme"];
            }
            if (items.ContainsKey("scheme"))
            {
                return items["scheme"];
            }
            throw new Exception("В ответе не найдены данные по провайдеру");
        }
    }

    private async Task<ApplicationUser> CreateNewUserAsync(string provider, string providerUserId, string email)
    {
        var login = string.IsNullOrEmpty(email) ? providerUserId : email;
        var (user, error) = await CreateUser(login, password: string.Empty, phone: string.Empty, externalSystemName: provider, externalSystemIdentifier: providerUserId);

        if(!string.IsNullOrEmpty(error))
        {
            throw new Exception("Ошибка регистрации пользователя:" + error);
        }
        return user;
    }
}
