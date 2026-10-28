using LinkVaultApi.Models;
using LinkVaultApi.Services;
using System.Security.Claims;

namespace LinkVaultApi.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var endpoint = context.GetEndpoint();
        
        // Skip audit for certain endpoints
        if (endpoint?.Metadata.GetMetadata<SkipAuditAttribute>() != null)
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        
        try
        {
            await _next(context);

            // Log based on response status and method
            if (ShouldLogRequest(context))
            {
                await LogRequestAsync(context, auditService);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audit middleware");
            throw;
        }
    }

    private bool ShouldLogRequest(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Log all write operations
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            // Skip auth endpoints
            if (path.Contains("/auth/")) return false;
            
            return true;
        }

        return false;
    }

    private async Task LogRequestAsync(HttpContext context, IAuditService auditService)
    {
        try
        {
            var userId = GetUserId(context);
            var orgId = GetOrganizationId(context);
            
            if (!orgId.HasValue) return;

            var (actionType, entityType, entityId) = ParseEndpoint(context);

            await auditService.LogAsync(
                actionType: actionType,
                entityType: entityType,
                entityId: entityId,
                entityName: null,
                description: $"{context.Request.Method} {context.Request.Path}",
                oldValues: null,
                newValues: null,
                organizationId: orgId.Value,
                userId: userId,
                ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry");
        }
    }

    private Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    private Guid? GetOrganizationId(HttpContext context)
    {
        var orgIdClaim = context.User.FindFirst("orgId");
        if (orgIdClaim != null && Guid.TryParse(orgIdClaim.Value, out var orgId))
        {
            return orgId;
        }
        return null;
    }

    private (AuditActionType actionType, ResourceEntityType entityType, Guid? entityId) ParseEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        AuditActionType actionType = method switch
        {
            "POST" => AuditActionType.Created,
            "PUT" or "PATCH" => AuditActionType.Updated,
            "DELETE" => AuditActionType.Deleted,
            _ => AuditActionType.Viewed
        };

        ResourceEntityType entityType = ResourceEntityType.System;
        Guid? entityId = null;

        if (path.Contains("/directories"))
        {
            entityType = ResourceEntityType.Directory;
            entityId = ExtractIdFromPath(path);
        }
        else if (path.Contains("/resources"))
        {
            entityType = ResourceEntityType.Resource;
            entityId = ExtractIdFromPath(path);
        }
        else if (path.Contains("/links"))
        {
            entityType = ResourceEntityType.Link;
            entityId = ExtractIdFromPath(path);
        }
        else if (path.Contains("/users"))
        {
            entityType = ResourceEntityType.User;
            entityId = ExtractIdFromPath(path);
        }
        else if (path.Contains("/roles"))
        {
            entityType = ResourceEntityType.Settings;
        }

        return (actionType, entityType, entityId);
    }

    private Guid? ExtractIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (Guid.TryParse(segment, out var id))
            {
                return id;
            }
        }
        return null;
    }
}

// Attribute to skip audit logging for specific endpoints
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipAuditAttribute : Attribute
{
}
