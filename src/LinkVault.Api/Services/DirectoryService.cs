using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Directories;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class DirectoryService : IDirectoryService
{
    private readonly ApplicationDbContext _context;

    public DirectoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DirectoryDto> CreateAsync(Guid userId, CreateDirectoryRequest request)
    {
        if (request.ParentId.HasValue)
        {
            var parent = await _context.Directories.FindAsync(request.ParentId.Value);
            if (parent == null || parent.OwnerId != userId)
                throw new NotFoundException("Parent directory not found");
        }

        var directory = new UserDirectory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            ParentDirectoryId = request.ParentId,
            IsPublic = request.IsPublic,
            Slug = GenerateSlug(request.Name),
            CreatedAt = DateTime.UtcNow
        };

        _context.Directories.Add(directory);
        await _context.SaveChangesAsync();

        return MapToDto(directory);
    }

    public async Task<DirectoryDto> GetByIdAsync(Guid userId, Guid id)
    {
        var directory = await _context.Directories
            .Include(d => d.SubDirectories)
            .Include(d => d.Links)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (directory == null)
            throw new NotFoundException("Directory not found");

        if (directory.OwnerId != userId && !directory.IsPublic)
            throw new UnauthorizedException("Access denied");

        return MapToDto(directory);
    }

    public async Task<PagedResult<DirectoryDto>> GetUserDirectoriesAsync(Guid userId, Guid? parentId, int page, int pageSize)
    {
        var query = _context.Directories
            .Include(d => d.SubDirectories)
            .Include(d => d.Links)
            .Where(d => d.OwnerId == userId);

        if (parentId.HasValue)
            query = query.Where(d => d.ParentDirectoryId == parentId);
        else
            query = query.Where(d => d.ParentDirectoryId == null);

        var totalCount = await query.CountAsync();

        var directories = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DirectoryDto>
        {
            Items = directories.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DirectoryDto> UpdateAsync(Guid userId, Guid id, UpdateDirectoryRequest request)
    {
        var directory = await _context.Directories.FindAsync(id);

        if (directory == null)
            throw new NotFoundException("Directory not found");

        if (directory.OwnerId != userId)
            throw new UnauthorizedException("Access denied");

        if (request.Name != null)
        {
            directory.Name = request.Name;
            directory.Slug = GenerateSlug(request.Name);
        }
        if (request.Description != null) directory.Description = request.Description;
        if (request.IsPublic.HasValue) directory.IsPublic = request.IsPublic.Value;
        if (request.ParentId.HasValue) directory.ParentDirectoryId = request.ParentId;

        directory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(directory);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var directory = await _context.Directories
            .Include(d => d.SubDirectories)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (directory == null)
            throw new NotFoundException("Directory not found");

        if (directory.OwnerId != userId)
            throw new UnauthorizedException("Access denied");

        if (directory.SubDirectories.Any())
            throw new ConflictException("Cannot delete directory with subdirectories");

        _context.Directories.Remove(directory);
        await _context.SaveChangesAsync();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("--", "-");
    }

    private static DirectoryDto MapToDto(UserDirectory directory)
    {
        return new DirectoryDto
        {
            Id = directory.Id,
            Name = directory.Name,
            Slug = directory.Slug,
            Description = directory.Description,
            ParentId = directory.ParentDirectoryId,
            IsPublic = directory.IsPublic,
            LinkCount = directory.Links?.Count ?? 0,
            SubDirectoryCount = directory.SubDirectories?.Count ?? 0,
            CreatedAt = directory.CreatedAt,
            UpdatedAt = directory.UpdatedAt
        };
    }
}
