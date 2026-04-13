using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using SevSharks.Identity.WebUI.Models;
using SevSharks.Identity.WebUI.Options;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace SevSharks.Identity.WebUI.Controllers
{
    public partial class AccountController
    {
        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var vm = await BuildLoginViewModelAsync(returnUrl);
            if (vm.RedirectToRegister)
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("Register");
            }

            if (vm.RedirectToChangeRoles)
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("ChangeRoles");
            }

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return ExternalLogin(vm.ExternalLoginScheme, returnUrl);
            }
            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //ApplicationUser user = await _userManager.FindByNameAsync(model.Login);  
                var result = await _signInManager.PasswordSignInAsync(model.Login, model.Password, model.AllowRememberLogin, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return await LoginInner();
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.Login, "invalid credentials"));
                ModelState.AddModelError("", AccountOptions.InvalidCredentialsErrorMessage);
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);

            async Task<IActionResult> LoginInner()
            {
                var user = await _userManager.FindByNameAsync(model.Login);
                ProcessAuthenticationProperties(model);
                return await SignInAndRedirect(user, model.ReturnUrl);
            }
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            AuthorizationRequest context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                // this is meant to short circuit the UI and only trigger the one external IdP
                return new LoginViewModel
                {
                    AllowRegister = AllowRegister,
                    EnableLocalLogin = false,
                    ReturnUrl = returnUrl,
                    Login = context.LoginHint,
                    ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } },
                    ClientId = context.Client.ClientId
                };
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null ||
                            (x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    Name = x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRegister = AllowRegister,
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Login = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                RedirectToRegister = GetBoolWithName(context, "redirect_to_register"),
                RedirectToChangeRoles = GetBoolWithName(context, "redirect_to_change_roles"),
                ClientId = context?.Client?.ClientId
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Login = model.Login;
            vm.AllowRememberLogin = model.AllowRememberLogin;
            return vm;
        }
    }
}