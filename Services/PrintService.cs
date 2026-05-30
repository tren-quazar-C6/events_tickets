using System.Text;
using System.Text.Json;
using events_tickets.Models;
using events_tickets.Responses;

namespace events_tickets.Services;

public class PrintService
{
    private readonly IHttpClientFactory _factory;

    public PrintService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<ServiceResponse<bool>> PrintTicketAsync(Ticket ticket)
    {
        try
        {
            var client = _factory.CreateClient("print");
            var body = JsonSerializer.Serialize(new
            {
                fullName = ticket.CustomerName,
                documentNumber = ticket.CustomerName,
                ticketCode = ticket.Code
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/print", content);

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<bool> { Success = false, Message = "Printer not available" };

            return new ServiceResponse<bool> { Success = true, Data = true };
        }
        catch
        {
            // Print failure is non-fatal; tickets are still valid
            return new ServiceResponse<bool> { Success = false, Message = "Printer not available" };
        }
    }
}