namespace events_tickets.Configuration;

public sealed class PrintServerOptions
{
    public const string SectionName = "PrintServer";
    public string BaseUrl { get; init; } = "http://localhost:9100";
    public string PrinterName { get; init; } = "Printer_USB_Printer_Port";
}