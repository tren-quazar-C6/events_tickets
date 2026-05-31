using System.Text;
using System.Text.Json;
using events_tickets.Configuration;
using events_tickets.Models;
using events_tickets.Responses;
using Microsoft.Extensions.Options;

namespace events_tickets.Services;

public class PrintService : IPrintService
{
    private readonly IHttpClientFactory _factory;
    private readonly PrintServerOptions _options;

    public PrintService(IHttpClientFactory factory, IOptions<PrintServerOptions> options)
    {
        _factory = factory;
        _options = options.Value;
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
                ticketCode = ticket.CodigoUnico
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/print/ticket", content);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
