using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SevSharks.Identity.BusinessLogic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SevSharks.Identity.BusinessLogic.Services;

///<summary>
/// Сервис синхронизации пользователей через webhook
///</summary>
public class UserSyncWebhookService : IUserSyncWebhookService
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    public const string WebhookClientName = "WebhookClient";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserSyncWebhookService> _logger;
    public UserSyncWebhookService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UserSyncWebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyUserChangedAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var webhookUrl = _configuration["UserSync:WebhookUrl"];
        var apiKey = _configuration["UserSync:ApiKey"];
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("UserSync:WebhookUrl не настроен. Синхронизация пропущена для пользователя {UserId}", userId);
            return;
        }

        var rolesList = roles?.ToList() ?? new List<string>();
        var payload = new UserSyncRequest
        {
            IdentityUserId = userId,
            IsCandidate = rolesList.Contains(RolesConstants.Candidate),
            IsExpert = rolesList.Contains(RolesConstants.Expert),
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient(WebhookClientName);
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);
            }

            var response = await httpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook успешно отправлен для пользователя {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Webhook вернул статус {StatusCode} для пользователя {UserId}", 
                    (int)response.StatusCode, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке webhook для пользователя {UserId}, {WebHookUrl}", userId, webhookUrl);
        }
    }
}
