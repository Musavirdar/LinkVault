using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class ExportRequestService : IExportRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;

    public ExportRequestService(ApplicationDbContext context, IAuditService audit, IEmailService email)
    {
        _context = context;
        _audit = audit;
        _email = email;
    }

    public async Task<ExportRequestDto> CreateRequestAsync(Guid userId, CreateExportRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        if (user.OrganizationId == null)
            throw new ConflictException("Export requests are only for corporate accounts");

        var org = await _context.Organizations.FindAsync(user.OrganizationId)
            ?? throw new NotFoundException("Organization not found");

        // If org policy is Open, no request needed
        if (org.DataExportPolicy == DataExportPolicy.Open)
            throw new ConflictException("Your organization allows direct downloads. No export request needed.");

        // If org policy is Restricted, always deny
        if (org.DataExportPolicy == DataExportPolicy.Restricted)
            throw new UnauthorizedException("Your organization has restricted all data exports.");

        var file = await _context.Files.FindAsync(request.FileItemId)
            ?? throw new NotFoundException("File not found");

        var exportRequest = new ExportRequest
        {
            Id = Guid.NewGuid(),
            FileItemId = request.FileItemId,
            RequestedById = userId,
            Reason = request.Reason,
            OrganizationId = user.OrganizationId.Value,
            Status = ExportRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExportRequests.Add(exportRequest);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, AuditAction.ExportRequest, "ExportRequest",
            exportRequest.Id, file.OriginalFileName, organizationId: user.OrganizationId);

        return MapToDto(exportRequest, file.OriginalFileName, user.Username);
    }

    public async Task<ExportRequestDto> ApproveAsync(Guid adminUserId, Guid requestId, string? notes)
    {
        var (req, file, requester) = await GetAndValidateAdminAsync(adminUserId, requestId);

        req.Status = ExportRequestStatus.Approved;
        req.ReviewedById = adminUserId;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewNotes = notes;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(adminUserId, AuditAction.ExportApproved, "ExportRequest",
            req.Id, file.OriginalFileName, organizationId: req.OrganizationId);

        await _email.SendExportApprovedAsync(requester.Email, file.OriginalFileName);

        return MapToDto(req, file.OriginalFileName, requester.Username);
    }

    public async Task<ExportRequestDto> DenyAsync(Guid adminUserId, Guid requestId, string reason)
    {
        var (req, file, requester) = await GetAndValidateAdminAsync(adminUserId, requestId);

        req.Status = ExportRequestStatus.Denied;
        req.ReviewedById = adminUserId;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewNotes = reason;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(adminUserId, AuditAction.ExportDenied, "ExportRequest",
            req.Id, file.OriginalFileName, organizationId: req.OrganizationId);

        await _email.SendExportDeniedAsync(requester.Email, file.OriginalFileName, reason);

        return MapToDto(req, file.OriginalFileName, requester.Username);
    }

    public async Task<IEnumerable<ExportRequestDto>> GetPendingForOrgAsync(Guid adminUserId, Guid orgId)
    {
        var isAdmin = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == adminUserId && ur.OrganizationId == orgId && ur.Role!.Name == "Admin");

        if (!isAdmin) throw new UnauthorizedException("Admin privileges required");

        return await _context.ExportRequests
            .Include(er => er.FileItem)
            .Include(er => er.RequestedBy)
            .Where(er => er.OrganizationId == orgId && er.Status == ExportRequestStatus.Pending)
            .OrderBy(er => er.CreatedAt)
            .Select(er => MapToDto(er, er.FileItem.OriginalFileName, er.RequestedBy.Username))
            .ToListAsync();
    }

    public async Task<IEnumerable<ExportRequestDto>> GetMyRequestsAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new NotFoundException("User not found");

        return await _context.ExportRequests
            .Include(er => er.FileItem)
            .Where(er => er.RequestedById == userId)
            .OrderByDescending(er => er.CreatedAt)
            .Select(er => MapToDto(er, er.FileItem.OriginalFileName, user.Username))
            .ToListAsync();
    }

    private async Task<(ExportRequest Request, FileItem File, User Requester)> GetAndValidateAdminAsync(Guid adminUserId, Guid requestId)
    {
        var req = await _context.ExportRequests
            .Include(er => er.FileItem)
            .Include(er => er.RequestedBy)
            .FirstOrDefaultAsync(er => er.Id == requestId)
            ?? throw new NotFoundException("Export request not found");

        if (req.Status != ExportRequestStatus.Pending)
            throw new ConflictException("This request has already been reviewed");

        var isAdmin = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == adminUserId && ur.OrganizationId == req.OrganizationId && ur.Role!.Name == "Admin");

        if (!isAdmin) throw new UnauthorizedException("Admin privileges required");

        return (req, req.FileItem, req.RequestedBy);
    }

    private static ExportRequestDto MapToDto(ExportRequest er, string fileName, string username) => new()
    {
        Id = er.Id,
        FileItemId = er.FileItemId,
        FileName = fileName,
        RequestedById = er.RequestedById,
        RequestedByUsername = username,
        Reason = er.Reason,
        Status = er.Status,
        ReviewNotes = er.ReviewNotes,
        CreatedAt = er.CreatedAt,
        ReviewedAt = er.ReviewedAt
    };
}
