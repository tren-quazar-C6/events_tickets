using events_tickets.Contracts;
using events_tickets.Dtos;

namespace events_tickets.Services;

public interface IClienteService
{
    Task<ClienteDto> CrearAsync(CrearClienteRequest req);
    Task<ClienteDto?> ObtenerAsync(int id);
    Task<ClienteDto?> ObtenerPorDocumentoAsync(string numeroDocumento);
    Task<List<ClienteDto>> ListarAsync();
}
