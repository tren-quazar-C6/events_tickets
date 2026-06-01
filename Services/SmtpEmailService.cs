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
            const string appButacaUrl = "https://quasar.andrescortes.dev/";

            var accessBlock = string.IsNullOrWhiteSpace(venta.AccessPassword)
                ? """
                  <p style="margin:18px 0 0;color:#475569;font-size:14px;line-height:1.5">
                    Ya tienes una cuenta registrada. Puedes entrar al módulo de usuarios con tu correo para revisar tus tickets.
                  </p>
                  """
                : $"""
                   <div style="margin-top:18px;padding:14px;border:1px solid #cbd5e1;border-radius:10px;background:#f8fafc">
                     <div style="font-size:14px;color:#0f172a;font-weight:700;margin-bottom:8px">Acceso al módulo de usuarios</div>
                     <div style="font-size:14px;color:#334155;line-height:1.6">
                       Correo: <strong>{System.Net.WebUtility.HtmlEncode(venta.AccessEmail ?? venta.EmailCliente)}</strong><br/>
                       Contraseña temporal: <strong>{System.Net.WebUtility.HtmlEncode(venta.AccessPassword)}</strong>
                     </div>
                   </div>
                   """;

            var builder = new BodyBuilder
            {
                TextBody = $"Hola {venta.NombreCliente}, adjuntamos tus tickets para {venta.NombreEvento}."
                    + (string.IsNullOrWhiteSpace(venta.AccessPassword)
                        ? "\n\nYa tienes una cuenta registrada. Puedes entrar al link de la app con tu correo para revisar tus tickets."
                        : $"\n\nAcceso a la App Butaca\nCorreo: {venta.AccessEmail ?? venta.EmailCliente}\nContraseña temporal: {venta.AccessPassword}")
                    + $"\n\nAbrir App Butaca: {appButacaUrl}",
                HtmlBody = $"""
                            <div style="font-family:Arial,sans-serif;background:#f1f5f9;padding:24px">
                              <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:14px;overflow:hidden">
                                <div style="background:#1e293b;color:#ffffff;padding:18px 22px;font-size:20px;font-weight:700">
                                  Confirmación de compra
                                </div>
                                <div style="padding:22px">
                                  <p style="margin:0 0 12px;color:#0f172a;font-size:16px">
                                    Hola <strong>{System.Net.WebUtility.HtmlEncode(venta.NombreCliente ?? "Cliente")}</strong>,
                                  </p>
                                  <p style="margin:0 0 12px;color:#334155;font-size:14px;line-height:1.5">
                                    Tu compra se registró correctamente. Adjuntamos tus tickets en PDF para el evento:
                                  </p>
                                  <div style="padding:12px;border:1px solid #e2e8f0;border-radius:10px;background:#f8fafc">
                                    <div style="color:#0f172a;font-size:16px;font-weight:700">{System.Net.WebUtility.HtmlEncode(venta.NombreEvento ?? "Evento")}</div>
                                    <div style="color:#475569;font-size:13px;margin-top:4px">Venta #{venta.IdVenta} · {venta.CantidadTickets} ticket(s)</div>
                                  </div>
                                  {accessBlock}
                                  <div style="margin-top:18px">
                                    <a href="{appButacaUrl}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;padding:10px 14px;border-radius:8px;font-size:14px;font-weight:700">
                                      Ir a App Butaca
                                    </a>
                                  </div>
                                  <p style="margin:10px 0 0;color:#64748b;font-size:12px;line-height:1.5">
                                    Si el botón no funciona, usa este enlace:
                                    <a href="{appButacaUrl}" style="color:#2563eb">{appButacaUrl}</a>
                                  </p>
                                  <p style="margin:18px 0 0;color:#64748b;font-size:12px;line-height:1.5">
                                    Este correo fue generado automáticamente por Terminal de Taquilla.
                                  </p>
                                </div>
                              </div>
                            </div>
                            """
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
