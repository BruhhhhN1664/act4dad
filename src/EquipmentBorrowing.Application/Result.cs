namespace EquipmentBorrowing.Application;

public enum ErrorKind
{
    None,
    Validation,
    NotFound,
    Rejected,
    Conflict,
    ConnectionError,
    UnknownOutcome,
    Unexpected
}

public record Result<T>(bool Succeeded, ErrorKind Kind, string Message, T? Value)
{
    public static Result<T> Ok(T value, string message = "") => new(true, ErrorKind.None, message, value);
    public static Result<T> Fail(ErrorKind kind, string message) => new(false, kind, message, default);
}
