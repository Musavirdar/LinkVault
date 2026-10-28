using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }
    
    [MaxLength(100)]
    public string? ReasonRevoked { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    // Foreign keys
    [Required]
    public Guid UserId { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    public bool IsRevoked => RevokedAt != null;
    
    public bool IsActive => !IsRevoked && !IsExpired && !IsUsed;
}
