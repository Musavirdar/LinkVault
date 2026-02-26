using System.Security.Cryptography;
using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Links;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class LinkService : ILinkService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private const string ShortCodeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int DefaultShortCodeLength = 7;
    
    public LinkService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<LinkDto> CreateAsync(Guid userId, CreateLinkRequest request)
    {
        var shortCode = request.CustomShortCode ?? await GenerateUniqueShortCodeAsync();
        
        if (request.CustomShortCode != null)
        {
            var exists = await _context.Links.AnyAsync(l => l.ShortCode == request.CustomShortCode);
            if (exists)
                throw new ConflictException("Short code already exists");
        }
        
        var link = new Link
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.OriginalUrl,
            ShortCode = shortCode,
            Title = request.Title,
            Description = request.Description,
            UserId = userId,
            DirectoryId = request.DirectoryId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Links.Add(link);
        
        if (request.Tags != null && request.Tags.Count > 0)
        {
            foreach (var tagName in request.Tags.Distinct())
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.UserId == userId && t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { Id = Guid.NewGuid(), Name = tagName, UserId = userId };
                    _context.Tags.Add(tag);
                }
                link.LinkTags.Add(new LinkTag { LinkId = link.Id, TagId = tag.Id });
            }
        }
        
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(userId, link.Id);
    }
    
    public async Task<LinkDto> GetByIdAsync(Guid userId, Guid id)
    {
        var link = await _context.Links
            .Include(l => l.LinkDirectory)
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
            
        if (link == null)
            throw new NotFoundException("Link not found");
            
        return MapToDto(link);
    }
    
    public async Task<LinkDto?> GetByShortCodeAsync(string shortCode)
    {
        var link = await _context.Links
            .Include(l => l.LinkDirectory)
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .FirstOrDefaultAsync(l => l.ShortCode == shortCode);
            
        if (link == null)
            return null;
            
        return MapToDto(link);
    }
    
    public async Task<PagedResult<LinkDto>> GetUserLinksAsync(Guid userId, Guid? directoryId, int page, int pageSize)
    {
        var query = _context.Links
            .Include(l => l.LinkDirectory)
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.UserId == userId);
            
        if (directoryId.HasValue)
            query = query.Where(l => l.DirectoryId == directoryId);
            
        var totalCount = await query.CountAsync();
        
        var links = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return new PagedResult<LinkDto>
        {
            Items = links.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    
    public async Task<LinkDto> UpdateAsync(Guid userId, Guid id, UpdateLinkRequest request)
    {
        var link = await _context.Links
            .Include(l => l.LinkTags)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
            
        if (link == null)
            throw new NotFoundException("Link not found");
            
        if (request.OriginalUrl != null) link.OriginalUrl = request.OriginalUrl;
        if (request.Title != null) link.Title = request.Title;
        if (request.Description != null) link.Description = request.Description;
        if (request.DirectoryId.HasValue) link.DirectoryId = request.DirectoryId;
        if (request.ExpiresAt.HasValue) link.ExpiresAt = request.ExpiresAt;
        if (request.IsActive.HasValue) link.IsActive = request.IsActive.Value;
        link.UpdatedAt = DateTime.UtcNow;
        
        if (request.Tags != null)
        {
            link.LinkTags.Clear();
            foreach (var tagName in request.Tags.Distinct())
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.UserId == userId && t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { Id = Guid.NewGuid(), Name = tagName, UserId = userId };
                    _context.Tags.Add(tag);
                }
                link.LinkTags.Add(new LinkTag { LinkId = link.Id, TagId = tag.Id });
            }
        }
        
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(userId, id);
    }
    
    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        
        if (link == null)
            throw new NotFoundException("Link not found");
            
        _context.Links.Remove(link);
        await _context.SaveChangesAsync();
    }
    
    public async Task<string> ResolveAndTrackAsync(string shortCode)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.ShortCode == shortCode);
        
        if (link == null)
            throw new NotFoundException("Link not found");
            
        if (!link.IsActive)
            throw new NotFoundException("Link is inactive");
            
        if (link.ExpiresAt.HasValue && link.ExpiresAt < DateTime.UtcNow)
            throw new NotFoundException("Link has expired");
            
        link.ClickCount++;
        link.LastClickedAt = DateTime.UtcNow;
        
        var click = new LinkClick
        {
            Id = Guid.NewGuid(),
            LinkId = link.Id,
            ClickedAt = DateTime.UtcNow
        };
        
        _context.LinkClicks.Add(click);
        await _context.SaveChangesAsync();
        
        return link.OriginalUrl;
    }
    
    private async Task<string> GenerateUniqueShortCodeAsync()
    {
        string shortCode;
        do
        {
            shortCode = GenerateShortCode(DefaultShortCodeLength);
        } while (await _context.Links.AnyAsync(l => l.ShortCode == shortCode));
        
        return shortCode;
    }
    
    private static string GenerateShortCode(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => ShortCodeChars[b % ShortCodeChars.Length]).ToArray());
    }
    
    private LinkDto MapToDto(Link link)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        return new LinkDto
        {
            Id = link.Id,
            OriginalUrl = link.OriginalUrl,
            ShortCode = link.ShortCode,
            ShortUrl = $"{baseUrl}/{link.ShortCode}",
            Title = link.Title,
            Description = link.Description,
            DirectoryId = link.DirectoryId,
            DirectoryName = link.LinkDirectory?.Name,
            ClickCount = link.ClickCount,
            LastClickedAt = link.LastClickedAt,
            ExpiresAt = link.ExpiresAt,
            IsActive = link.IsActive,
            CreatedAt = link.CreatedAt,
            UpdatedAt = link.UpdatedAt,
            Tags = link.LinkTags.Select(lt => lt.Tag.Name).ToList()
        };
    }
}
