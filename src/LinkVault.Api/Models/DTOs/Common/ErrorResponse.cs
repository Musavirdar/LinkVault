namespace LinkVault.Api.Models.DTOs.Common;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? TraceId { get; set; }
}
