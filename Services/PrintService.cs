using System.Text;
using System.Text.Json;
using events_tickets.Configuration;
using events_tickets.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace events_tickets.Services;

public class PrintService : IPrintService
{
    private readonly IHttpClientFactory _factory;
    private readonly PrintServerOptions _options;
    private readonly ILogger<PrintService> _logger;

    public PrintService(IHttpClientFactory factory, IOptions<PrintServerOptions> options, ILogger<PrintService> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ImprimirAsync(Ticket ticket, string nombreEvento, DateTime fechaEvento, string nombreCliente, string numeroDocumento)
    {
        try
        {
            var client = _factory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl);
            var body = JsonSerializer.Serialize(new
            {
                eventName = nombreEvento,
                eventDate = fechaEvento.ToString("yyyy-MM-dd HH:mm"),
                customerName = nombreCliente,
                documentNumber = numeroDocumento,
                section = ticket.Zona,
                seatNumber = ticket.CodigoAsiento,
                ticketCode = ticket.CodigoUnico,
                qrToken = ticket.QrToken   // ADD THIS
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/print/ticket", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Print server error. Status={StatusCode}, Body={Body}, BaseUrl={BaseUrl}",
                    (int)response.StatusCode, responseBody, _options.BaseUrl);
                return false;
            }

            _logger.LogInformation("Ticket print sent successfully. TicketId={TicketId}, Seat={Seat}, Response={ResponseBody}",
                ticket.IdTicket, ticket.CodigoAsiento, responseBody);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Print request failed. BaseUrl={BaseUrl}, TicketId={TicketId}",
                _options.BaseUrl, ticket.IdTicket);
            return false;
        }
    }
}
