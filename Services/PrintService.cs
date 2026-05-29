using System.Net.Http.Json;
using events_tickets.Configuration;
using events_tickets.Models;
using Microsoft.Extensions.Options;

namespace events_tickets.Services;

public class PrintService : IPrintService
{
    private readonly HttpClient _http;
    private readonly PrintServerOptions _options;

    public PrintService(HttpClient http, IOptions<PrintServerOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<bool> ImprimirAsync(
        Ticket ticket,
        string nombreEvento,
        DateTime fechaEvento,
        string nombreCliente,
        string numeroDocumento)
    {
        var payload = new
        {
            eventName = nombreEvento,
            eventDate = fechaEvento.ToString("yyyy-MM-dd HH:mm"),
            customerName = nombreCliente,
            documentNumber = numeroDocumento,
            section = ticket.Zona,
            seatNumber = ticket.CodigoAsiento,
            ticketCode = ticket.CodigoUnico
        };

        var url = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), "print/ticket");
        var response = await _http.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }
}
