using System.Net;
using System.Net.Mail;
using events_tickets.Configuration;
using events_tickets.Dtos;
using Microsoft.Extensions.Options;

namespace events_tickets.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ITicketService _tickets;

    public SmtpEmailService(IOptions<EmailOptions> options, ITicketService tickets)
    {
        _options = options.Value;
        _tickets = tickets;
    }

    public async Task<bool> SendTicketsAsync(VentaDetalleDto venta)
    {
        if (!_options.IsConfigured || string.IsNullOrWhiteSpace(venta.EmailCliente))
            return false;

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = $"Tickets - {venta.NombreEvento}",
            Body = $"Hola {venta.NombreCliente}, adjuntamos tus tickets para {venta.NombreEvento}.",
            IsBodyHtml = false
        };
        message.To.Add(venta.EmailCliente);

        foreach (var ticket in venta.Tickets)
        {
            var pdf = await _tickets.GenerarPdfAsync(ticket.IdTicket);
            var stream = new MemoryStream(pdf);
            message.Attachments.Add(new Attachment(stream, $"ticket-{ticket.IdTicket}.pdf", "application/pdf"));
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        await client.SendMailAsync(message);
        return true;
    }
}
