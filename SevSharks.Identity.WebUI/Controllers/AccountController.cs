using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.DataAccess.Models;
using SevSharks.Identity.WebUI.Helpers;
using SevSharks.Identity.WebUI.Models;
using SevSharks.Identity.WebUI.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;
using System.Net.Http;
using SevSharks.Identity.WebUI.Models.Captcha;

namespace SevSharks.Identity.WebUI.Controllers
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    public partial class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IConfiguration _configuration;
        private readonly CreateUserService _createUserService;
        private readonly ExternalSystemAccountService _externalSystemAccountService;
        private readonly ILogger<AccountController> _logger;

        /// <summary>
        /// 
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEventService events,
            CreateUserService createUserService,
            IConfiguration configuration,
            ExternalSystemAccountService externalSystemAccountService,
            ILogger<AccountController> logger)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _signInManager = signInManager;
            _userManager = userManager;
            _createUserService = createUserService;
            _configuration = configuration;
            _externalSystemAccountService = externalSystemAccountService;
            _logger = logger;
        }

        /// <summary>
        /// Lockout
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        #region Private   

        private async Task<bool> ValidateCaptcha()
        {
            var response = Request.Form[_configuration["CSP:response"]];
            var secretKey = _configuration["CSP:secretkey"];
            var url = string.Format(_configuration["CSP:recaptchaUrl"], secretKey, response);
            var client = new HttpClient { BaseAddress = new Uri(url) };

            var result = await client.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                var validCaptcha = JsonConvert.DeserializeObject<ValidCaptcha>(await result.Content.ReadAsStringAsync());
                if (validCaptcha == null || validCaptcha.Success == false)
                {
                    return false;
                }
                return true;
            }

            throw new Exception(result.StatusCode.ToString());
        }

        private async Task<(ApplicationUser, string)> CreateUser(string login,
            string password,
            string phone,
            string[] roles = null,
            string externalSystemIdentifier = null, string externalSystemName = null)
        {
            ApplicationUser user = null;
            string error = string.Empty;
            try
            {
                user = await _userManager.FindByNameAsync(login);
                if (user != null)
                {
                    return (null, $"Пользователь с email {login} уже существует");
                }

                var userDto = new CreateUserDto
                {
                    UserName = login,
                    Email = login,
                    EmailConfirmed = false,
                    PhoneNumber = phone,
                    PhoneNumberConfirmed = false,
                    ExternalSystemIdentifier = externalSystemIdentifier,
                    ExternalSystemName = externalSystemName,
                    Password = password,
                    Roles = roles
                };

                user = await _createUserService.CreateUser(userDto);
            }
            catch (Exception e)
            {
                error = e.Message;
                return (user, error);
            }

            return (user, error);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private async Task<IActionResult> SignInAndRedirect(ApplicationUser user, string returnUrl)
        {
            // issue authentication cookie with subject ID and username
            await _signInManager.SignInAsync(user, isPersistent: false);

            // make sure the returnUrl is still valid, and if so redirect back to authorize endpoint or a local page
            // the IsLocalUrl check is only necessary if you want to support additional local pages, otherwise IsValidReturnUrl is more strict
            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect(Url.Content("~/"));
        }

        private string ComposeErrorFromErrorList(IList<string> errors)
        {
            string result = string.Empty;
            if (errors.Count == 0)
            {
                return result;
            }
            foreach (var errorMessage in errors)
            {
                result += errorMessage + ";";
            }
            result.TrimEnd(';');
            return result;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider)
                {
                    /*
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }*/
                }
            }

            return vm;
        }

        private bool GetBoolWithName(AuthorizationRequest request, string name)
        {
            if (request?.Parameters == null || request.Parameters.Count == 0)
            {
                return false;
            }

            var requestParameter = request.Parameters[name];
            if (string.IsNullOrEmpty(requestParameter))
            {
                return false;
            }

            if (bool.TryParse(requestParameter, out var result))
            {
                return result;
            }

            return false;
        }

        private void ProcessAuthenticationProperties(LoginViewModel model)
        {
            //TODO: разобраться как работает
            /*
            // only set explicit expiration here if user chooses "remember me". 
            // otherwise we rely upon expiration configured in cookie middleware.
            AuthenticationProperties props = null;
            if (AccountOptions.AllowRememberLogin && model.AllowRememberLogin)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                };
            };
            */
        }

        private string GetCurrentUserId()
        {
            var firstClaim = User?.Claims?.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(firstClaim))
            {
                return User?.Claims?.Where(c => c.Type == "sub").Select(c => c.Value)
                    .FirstOrDefault();
            }
            return firstClaim;
        }

        private bool AllowRegister
        {
            get
            {
                return bool.TryParse(_configuration["AllowRegister"], out var result) && result;
            }
        }
        #endregion
    }
}
