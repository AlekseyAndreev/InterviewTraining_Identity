using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SevSharks.Identity.BusinessLogic.Services;

/// <summary>
/// Интерфейс сервиса синхронизации пользователей через webhook
/// </summary>
public interface IUserSyncWebhookService
{
    /// <summary>
    /// Отправить уведомление о создании/обновлении пользователя
    /// </summary>
    Task NotifyUserChangedAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);
}
