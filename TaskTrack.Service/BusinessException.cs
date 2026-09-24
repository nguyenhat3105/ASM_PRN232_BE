namespace TaskTrack.Service;

public sealed class BusinessException(int status, string message, string? field = null) : Exception(message)
{
    public int Status { get; } = status;
    public string? Field { get; } = field;
    public static BusinessException NotFound(string resource) => new(404, $"{resource} was not found.");
    public static BusinessException Invalid(string field, string message) => new(400, message, field);
}
