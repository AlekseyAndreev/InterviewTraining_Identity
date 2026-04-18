using Microsoft.AspNetCore.Mvc;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.BusinessLogic.Services;
using SevSharks.Identity.WebUI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace SevSharks.Identity.WebUI.Controllers
{
    public partial class AccountController
    {
        /// <summary>
        /// Допустимые роли для регистрации
        /// </summary>
        private static readonly HashSet<string> AllowedRoles = new HashSet<string> { RolesConstants.Candidate, RolesConstants.Expert };

        /// <summary>
        /// Show register page
        /// </summary>
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

        /// <summary>
        /// Post register page
        /// </summary>
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

                    //TODO: GenerateEmailConfirmationTokenAsync
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    return await SignInAndRedirect(user, registerViewModel.ReturnUrl);
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
    }
}
