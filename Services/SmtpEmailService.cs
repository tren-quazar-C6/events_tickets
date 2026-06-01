using events_tickets.Dtos;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace events_tickets.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ITicketService _tickets;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration config,
        ITicketService tickets,
        ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _tickets = tickets;
        _logger = logger;
    }

    public async Task<bool> SendTicketsAsync(VentaDetalleDto venta)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? _config["Email:Host"] ?? "smtp.gmail.com";
        var smtpPort = int.TryParse(_config["Email:SmtpPort"] ?? _config["Email:Port"], out var p) ? p : 587;
        var smtpUser = _config["Email:User"] ?? _config["Email:Username"] ?? "";
        var smtpPass = (_config["Email:Password"] ?? string.Empty).Replace(" ", string.Empty);
        var enableSsl = bool.TryParse(_config["Email:EnableSsl"], out var ssl) ? ssl : true;

        if (string.IsNullOrWhiteSpace(venta.EmailCliente) ||
            string.IsNullOrWhiteSpace(smtpUser) ||
            string.IsNullOrWhiteSpace(smtpPass))
        {
            _logger.LogWarning("Email skipped. Recipient={Recipient}, UserConfigured={UserConfigured}, PassConfigured={PassConfigured}",
                venta.EmailCliente,
                !string.IsNullOrWhiteSpace(smtpUser),
                !string.IsNullOrWhiteSpace(smtpPass));
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtpUser));
            message.To.Add(MailboxAddress.Parse(venta.EmailCliente));
            message.Subject = $"Tickets - {venta.NombreEvento}";

            var builder = new BodyBuilder
            {
                TextBody = $"Hola {venta.NombreCliente}, adjuntamos tus tickets para {venta.NombreEvento}."
            };

            foreach (var ticket in venta.Tickets)
            {
                var pdf = await _tickets.GenerarPdfAsync(ticket.IdTicket);
                builder.Attachments.Add($"ticket-{ticket.IdTicket}.pdf", pdf, ContentType.Parse("application/pdf"));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var socketOption = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(smtpHost, smtpPort, socketOption);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Tickets email sent to {Recipient} for venta {VentaId}",
                venta.EmailCliente, venta.IdVenta);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tickets email failed for venta {VentaId} to {Recipient}",
                venta.IdVenta, venta.EmailCliente);
            return false;
        }
    }
}
