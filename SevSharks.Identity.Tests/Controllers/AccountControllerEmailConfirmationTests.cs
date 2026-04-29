using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Identity;
using Moq;
using SevSharks.Identity.DataAccess.Models;
using SevSharks.Identity.WebUI.Controllers;
using SevSharks.Identity.WebUI.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using SevSharks.Identity.BusinessLogic;
using SevSharks.Identity.BusinessLogic.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace SevSharks.Identity.Tests.Controllers;

public class AccountControllerEmailConfirmationTests : BaseUnitTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IIdentityServerInteractionService> _interactionMock;
    private readonly Mock<IClientStore> _clientStoreMock;
    private readonly Mock<IAuthenticationSchemeProvider> _schemeProviderMock;
    private readonly Mock<IEventService> _eventsMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<UserService> _userServiceMock;
    private readonly Mock<ExternalSystemAccountService> _externalSystemAccountServiceMock;
    private readonly Mock<IUserSyncWebhookService> _userSyncWebhookServiceMock;
    private readonly Mock<SevSharks.Identity.WebUI.Services.IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerEmailConfirmationTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsPrincipalFactoryMock.Object,
            null, null, null, null);

        _emailSenderMock = new Mock<SevSharks.Identity.WebUI.Services.IEmailSender>();
        _interactionMock = new Mock<IIdentityServerInteractionService>();
        _clientStoreMock = new Mock<IClientStore>();
        _schemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        _eventsMock = new Mock<IEventService>();
        _configurationMock = new Mock<IConfiguration>();
        _userServiceMock = new Mock<UserService>(null, null);
        _externalSystemAccountServiceMock = new Mock<ExternalSystemAccountService>(null, null);
        _userSyncWebhookServiceMock = new Mock<IUserSyncWebhookService>();
        _loggerMock = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(
            _interactionMock.Object,
            _clientStoreMock.Object,
            _schemeProviderMock.Object,
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _eventsMock.Object,
            _userServiceMock.Object,
            _configurationMock.Object,
            _externalSystemAccountServiceMock.Object,
            _userSyncWebhookServiceMock.Object,
            _emailSenderMock.Object,
            _loggerMock.Object);

        // Setup TempData for the controller
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionary = new TempDataDictionary(
            new DefaultHttpContext(),
            tempDataProvider.Object);

        _controller.TempData = tempDataDictionary;

        // Setup UrlHelper for the controller (needed for EmailConfirmationLink)
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/Account/ConfirmEmail?userId=test&code=test");
        _controller.Url = urlHelperMock.Object;

        // Setup ControllerContext with HttpContext (needed for Request.Scheme)
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsEmailAndRedirectsToLogin()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", Email = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "valid-code"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ConfirmEmail(user.Id, "valid-code") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
        Assert.Equal("Ваш email успешно подтверждён. Теперь вы можете войти в систему.",
            _controller.TempData["EmailConfirmationMessage"]);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsErrorAndRedirectsToLogin()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", Email = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "invalid-code"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act
        var result = await _controller.ConfirmEmail(user.Id, "invalid-code") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
        Assert.Equal("Ошибка при подтверждении email. Пожалуйста, попробуйте ещё раз.",
            _controller.TempData["EmailConfirmationMessage"]);
    }

    [Fact]
    public async Task ConfirmEmail_WithNullUserId_RedirectsToLogin()
    {
        // Act
        var result = await _controller.ConfirmEmail(null, "some-code") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }

    [Fact]
    public async Task ConfirmEmail_WithNullCode_RedirectsToLogin()
    {
        // Act
        var result = await _controller.ConfirmEmail("user-id", null) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }

    [Fact]
    public async Task ConfirmEmail_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent-id")).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.ConfirmEmail("nonexistent-id", "some-code");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WithUnconfirmedUser_SendsEmailAndRedirectsToLogin()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", Email = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("new-token");

        var model = new EmailConfirmationRequiredViewModel { Email = user.Email, ReturnUrl = "/home" };

        // Act
        var result = await _controller.ResendConfirmationEmail(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
        _emailSenderMock.Verify(
            s => s.SendEmailConfirmationAsync(user.Email, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WithConfirmedUser_DoesNotSendEmailAndRedirectsToLogin()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", Email = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var model = new EmailConfirmationRequiredViewModel { Email = user.Email, ReturnUrl = "/home" };

        // Act
        var result = await _controller.ResendConfirmationEmail(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        _emailSenderMock.Verify(
            s => s.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WithNonexistentUser_DoesNotSendEmailAndRedirectsToLogin()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((ApplicationUser)null);

        var model = new EmailConfirmationRequiredViewModel { Email = "nonexistent@test.com", ReturnUrl = "/home" };

        // Act
        var result = await _controller.ResendConfirmationEmail(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        _emailSenderMock.Verify(
            s => s.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WithEmptyEmail_RedirectsToLogin()
    {
        // Arrange
        var model = new EmailConfirmationRequiredViewModel { Email = "", ReturnUrl = "/home" };

        // Act
        var result = await _controller.ResendConfirmationEmail(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }
}