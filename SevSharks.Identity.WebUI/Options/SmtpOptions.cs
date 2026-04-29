namespace SevSharks.Identity.WebUI.Options;

///<summary>
/// SMTP configuration options for email sending
///</summary>
public class SmtpOptions
{
    ///<summary>
    /// SMTP server host (e.g., smtp.yandex.ru)
    ///</summary>
    public string Host { get; set; } = "smtp.yandex.ru";

    ///<summary>
    /// SMTP server port (typically 587 for TLS/STARTTLS)
    ///</summary>
    public int Port { get; set; } = 587;

    ///<summary>
    /// Sender email address (e.g., interview.training@yandex.com)
    ///</summary>
    public string SenderEmail { get; set; } = string.Empty;

    ///<summary>
    /// Sender display name (e.g., "Interview Training")
    ///</summary>
    public string SenderName { get; set; } = string.Empty;

    ///<summary>
    /// Username for SMTP authentication (usually same as sender email for Yandex)
    ///</summary>
    public string Username { get; set; } = string.Empty;

    ///<summary>
    /// Password or app-specific password for SMTP authentication
    ///</summary>
    public string Password { get; set; } = string.Empty;

    ///<summary>
    /// Whether to use SSL/TLS encryption (recommended: true)
    ///</summary>
    public bool EnableSsl { get; set; } = true;

    ///<summary>
    /// Whether to use STARTTLS (recommended: true for port 587)
    ///</summary>
    public bool UseStartTls { get; set; } = true;

    ///<summary>
    /// Timeout in milliseconds for SMTP operations
    ///</summary>
    public int TimeoutMs { get; set; } = 30000;
}
