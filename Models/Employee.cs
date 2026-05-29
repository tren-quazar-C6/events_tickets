namespace events_tickets.Models;

public class Employee
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Position { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
