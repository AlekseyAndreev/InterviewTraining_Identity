using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevSharks.Identity.WebUI.Options;

namespace SevSharks.Identity.WebUI.Services;

///<summary>
/// SMTP-based email sender implementation using Yandex mail server
///</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly SmtpOptions _smtpOptions;

    public SmtpEmailSender(
        ILogger<SmtpEmailSender> logger,
        IOptions<SmtpOptions> smtpOptions)
    {
        _logger = logger;
        _smtpOptions = smtpOptions.Value;
    }

    ///<summary>
    /// Sends an email confirmation message
    ///</summary>
    public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Подтверждение email адреса";
        var htmlMessage = $@"
           <html>
           <body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
               <h2 style=""color: #333;"">Подтверждение email адреса</h2>
               <p>Здравствуйте!</p>
               <p>Для подтверждения вашего email адреса, пожалуйста, нажмите на кнопку ниже:</p>
               <div style=""text-align: center; margin: 30px 0;"">
                   <a href=""{confirmationLink}"" 
                       style=""background-color: #4CAF50; 
                              color: white; 
                              padding: 12px 30px; 
                              text-decoration: none; 
                              border-radius: 4px;
                              font-size: 16px;"">
                        Подтвердить email
                   </a>
               </div>
               <p>Или скопируйте и вставьте следующую ссылку в адресную строку браузера:</p>
               <p style=""word-break: break-all; color: #666;"">{confirmationLink}</p>
               <hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;"">
               <p style=""color: #888; font-size: 12px;"">
                    Если вы не регистрировались на нашем сайте, просто проигнорируйте это письмо.
               </p>
           </body>
           </html>";

        await SendEmailAsync(email, subject, htmlMessage);
    }

    ///<summary>
    /// Sends a generic email with HTML content
    ///</summary>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.SenderEmail))
        {
            _logger.LogError("SMTP SenderEmail is not configured");
            throw new InvalidOperationException("SMTP sender email is not configured");
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpOptions.SenderEmail, _smtpOptions.SenderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new MailAddress(email));

            using var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = _smtpOptions.TimeoutMs
            };

            // Configure credentials
            if (!string.IsNullOrWhiteSpace(_smtpOptions.Username))
            {
                smtpClient.Credentials = new NetworkCredential(
                    _smtpOptions.Username,
                    _smtpOptions.Password);
            }

            // For Yandex SMTP with port 587, use STARTTLS
            if (_smtpOptions.UseStartTls && _smtpOptions.Port == 587)
            {
                smtpClient.EnableSsl = true;
            }

            _logger.LogInformation(
                "Sending email to {Email} with subject: {Subject} via SMTP {Host}:{Port}",
                email, subject, _smtpOptions.Host, _smtpOptions.Port);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation(
                "Successfully sent email to {Email} with subject: {Subject}",
                email, subject);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex,
                "SMTP error while sending email to {Email}: {ErrorCode} - {Message}",
                email, ex.StatusCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while sending email to {Email}: {Message}",
                email, ex.Message);
            throw;
        }
    }
}
