namespace StudentCouncil.Application.Common.Exceptions;

/// <summary>401 — missing or invalid credentials. The <c>code</c> can be specialised
/// (e.g. <c>invalid_credentials</c>, <c>account_inactive</c>) without leaking which one applies.</summary>
public sealed class UnauthorizedException : AppException
{
    public override string Code { get; }
    public override int StatusCode => 401;

    public UnauthorizedException(string message = "Authentication is required.", string code = "unauthorized")
        : base(message)
    {
        Code = code;
    }
}

/// <summary>423 — the account is temporarily locked after too many failed sign-in attempts.</summary>
public sealed class LockedException : AppException
{
    public override string Code => "account_locked";
    public override int StatusCode => 423;

    public LockedException(string message = "The account is temporarily locked. Please try again later.")
        : base(message)
    {
    }
}

/// <summary>403 — the caller must change their temporary password before doing anything else.</summary>
public sealed class PasswordChangeRequiredException : AppException
{
    public override string Code => "password_change_required";
    public override int StatusCode => 403;

    public PasswordChangeRequiredException(string message = "You must change your password before continuing.")
        : base(message)
    {
    }
}

/// <summary>403 — authenticated but not allowed.</summary>
public sealed class ForbiddenException : AppException
{
    public override string Code => "forbidden";
    public override int StatusCode => 403;

    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message)
    {
    }
}

/// <summary>404 — the requested resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public override string Code => "not_found";
    public override int StatusCode => 404;

    public NotFoundException(string message = "The requested resource was not found.") : base(message)
    {
    }

    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
    }
}

/// <summary>400 — a bad request that carries a specific machine code (e.g. <c>invalid_reset_token</c>).</summary>
public sealed class BadRequestException : AppException
{
    public override string Code { get; }
    public override int StatusCode => 400;

    public BadRequestException(string message, string code = "bad_request") : base(message)
    {
        Code = code;
    }
}

/// <summary>409 — business conflict; the <c>code</c> can be specialised (e.g. <c>invalid_status_transition</c>).</summary>
public sealed class ConflictException : AppException
{
    public override string Code { get; }
    public override int StatusCode => 409;

    public ConflictException(string message = "The request conflicts with the current state.", string code = "conflict")
        : base(message)
    {
        Code = code;
    }
}

/// <summary>413 — the uploaded file exceeds the allowed size limit (spec 8).</summary>
public sealed class FileTooLargeException : AppException
{
    public override string Code => "payload_too_large";
    public override int StatusCode => 413;

    public FileTooLargeException(string message = "The uploaded file is too large.") : base(message)
    {
    }
}
