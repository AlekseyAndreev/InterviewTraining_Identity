using System.Text.Json.Serialization;

namespace SevSharks.Identity.BusinessLogic.Models;

/// <summary>
/// Модель запроса синхронизации пользователя
/// </summary>
public class UserSyncRequest
{
    /// <summary>
    /// Идентификатор пользователя в IdentityServer
    /// </summary>
    [JsonPropertyName("identityUserId")]
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>
    /// Является ли кандидатом
    /// </summary>
    [JsonPropertyName("isCandidate")]
    public bool IsCandidate { get; set; }

    /// <summary>
    /// Является ли экспертом
    /// </summary>
    [JsonPropertyName("isExpert")]
    public bool IsExpert { get; set; }
}
