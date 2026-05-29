namespace events_tickets.Contracts;

public record CrearVentaRequest(
    int IdEvento,
    int IdCliente,
    int IdStaff,
    List<int> IdEventoAsientos,
    string? Notas = null
);

public record CancelarVentaRequest(string Motivo);
