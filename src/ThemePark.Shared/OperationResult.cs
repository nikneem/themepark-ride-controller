namespace ThemePark.Shared;

public enum OperationErrorKind { None, NotFound, Conflict, BadRequest }

public sealed record OperationResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public OperationErrorKind ErrorKind { get; init; }

    public static OperationResult Success() => new() { IsSuccess = true };
    public static OperationResult NotFound(string? error = null) => new() { ErrorKind = OperationErrorKind.NotFound, Error = error };
    public static OperationResult Conflict(string? error = null) => new() { ErrorKind = OperationErrorKind.Conflict, Error = error };
    public static OperationResult BadRequest(string? error = null) => new() { ErrorKind = OperationErrorKind.BadRequest, Error = error };
}

public sealed record OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public OperationErrorKind ErrorKind { get; init; }

    public static OperationResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static OperationResult<T> NotFound(string? error = null) => new() { ErrorKind = OperationErrorKind.NotFound, Error = error };
    public static OperationResult<T> Conflict(string? error = null) => new() { ErrorKind = OperationErrorKind.Conflict, Error = error };
    public static OperationResult<T> BadRequest(string? error = null) => new() { ErrorKind = OperationErrorKind.BadRequest, Error = error };
}
