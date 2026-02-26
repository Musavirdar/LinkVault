using System.Net;

namespace LinkVault.Api.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ErrorCode { get; }

    public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string errorCode = "BAD_REQUEST") : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message, HttpStatusCode.NotFound, "NOT_FOUND") { }
    public NotFoundException(string entity, Guid id) : base($"{entity} with ID '{id}' was not found.", HttpStatusCode.NotFound, "NOT_FOUND") { }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(message, HttpStatusCode.BadRequest, "BAD_REQUEST") { }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, HttpStatusCode.Unauthorized, "UNAUTHORIZED") { }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Access denied") : base(message, HttpStatusCode.Forbidden, "FORBIDDEN") { }
}

public class ConflictException : ApiException
{
    public ConflictException(string message) : base(message, HttpStatusCode.Conflict, "CONFLICT") { }
}
