using LinkVault.Api.Models.Entities;

namespace LinkVault.Api.Services.Interfaces;

public interface IExportRequestService
{
    Task<ExportRequestDto> CreateRequestAsync(Guid userId, CreateExportRequestDto request);
    Task<ExportRequestDto> ApproveAsync(Guid adminUserId, Guid requestId, string? notes);
    Task<ExportRequestDto> DenyAsync(Guid adminUserId, Guid requestId, string reason);
    Task<IEnumerable<ExportRequestDto>> GetPendingForOrgAsync(Guid adminUserId, Guid orgId);
    Task<IEnumerable<ExportRequestDto>> GetMyRequestsAsync(Guid userId);
}

public class ExportRequestDto
{
    public Guid Id { get; set; }
    public Guid FileItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid RequestedById { get; set; }
    public string RequestedByUsername { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public ExportRequestStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateExportRequestDto
{
    public Guid FileItemId { get; set; }
    public string? Reason { get; set; }
}
