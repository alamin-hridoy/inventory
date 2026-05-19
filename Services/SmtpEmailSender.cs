using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace InventoryPilot.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = _configuration["Email:Smtp:Host"];
        var from = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("Email is not configured. Confirmation email for {Email}: {Subject} {HtmlMessage}", email, subject, htmlMessage);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(host, ReadInt("Email:Smtp:Port", 587))
        {
            EnableSsl = ReadBool("Email:Smtp:EnableSsl", true)
        };

        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message);
    }

    private int ReadInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var value) ? value : fallback;

    private bool ReadBool(string key, bool fallback) =>
        bool.TryParse(_configuration[key], out var value) ? value : fallback;
}
