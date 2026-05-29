namespace events_tickets.Contracts;

public record CrearClienteRequest(
    string Nombre,
    string NumeroDocumento,
    string? Email = null,
    string? Telefono = null
);