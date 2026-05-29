namespace events_tickets.Infrastructure;

public class ServiceResponse<T>
{
    public bool Success { get; }
    public string? Message { get; }
    public T? Data { get; }
    public IReadOnlyList<string>? Errors { get; }

    private ServiceResponse(bool success, string? message, T? data, IReadOnlyList<string>? errors)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public static ServiceResponse<T> Ok(T data, string? message = null) =>
        new(true, message, data, null);

    public static ServiceResponse<T> Fail(string message, params string[] errors) =>
        new(false, message, default, errors.Length > 0 ? errors : null);
}