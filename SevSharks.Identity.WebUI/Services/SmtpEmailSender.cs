using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SevSharks.Identity.WebUI.Options;

namespace SevSharks.Identity.WebUI.Services;

///<summary>
/// SMTP-based email sender implementation using Yandex mail server (MailKit)
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

        if (string.IsNullOrWhiteSpace(_smtpOptions.Username) || string.IsNullOrWhiteSpace(_smtpOptions.Password))
        {
            _logger.LogError("SMTP credentials are not configured");
            throw new InvalidOperationException("SMTP credentials (Username/Password) are not configured");
        }

        _logger.LogInformation(
            "SMTP Config: Host={Host}, Port={Port}, Username={Username}",
            _smtpOptions.Host, _smtpOptions.Port, _smtpOptions.Username);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpOptions.SenderName, _smtpOptions.SenderEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Для Яндекса используем порт 587 с STARTTLS или порт 465 с SSL
            var secureSocketOptions = _smtpOptions.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                _smtpOptions.Host,
                _smtpOptions.Port,
                secureSocketOptions);

            // Аутентификация
            await client.AuthenticateAsync(
                _smtpOptions.Username,
                _smtpOptions.Password);

            _logger.LogInformation(
                "Sending email to {Email} with subject: {Subject} via SMTP {Host}:{Port}",
                email, subject, _smtpOptions.Host, _smtpOptions.Port);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Successfully sent email to {Email} with subject: {Subject}",
                email, subject);
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
