namespace StudentCouncil.Application.Common.Models;

/// <summary>Lightweight success/failure wrapper for use-cases that prefer not to throw.</summary>
public class Result
{
    public bool Succeeded { get; }
    public string? Error { get; }
    public string? Code { get; }

    protected Result(bool succeeded, string? error, string? code)
    {
        Succeeded = succeeded;
        Error = error;
        Code = code;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? code = null) => new(false, error, code);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, string? error, string? code)
        : base(succeeded, error, code)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static new Result<T> Failure(string error, string? code = null) => new(false, default, error, code);
}
