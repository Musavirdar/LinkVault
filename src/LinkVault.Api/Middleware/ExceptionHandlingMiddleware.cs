using System.Net;
using System.Text.Json;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Common;

namespace LinkVault.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            UnauthorizedException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.BadRequest,
            ConflictException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };
        
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        var response = new ErrorResponse
        {
            Message = exception.Message,
            Details = statusCode == HttpStatusCode.InternalServerError ? null : exception.Message,
            TraceId = context.TraceIdentifier
        };
        
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
