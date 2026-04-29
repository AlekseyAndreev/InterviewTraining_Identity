using System.Threading.Tasks;

namespace SevSharks.Identity.WebUI.Services;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string email, string confirmationLink);
}
