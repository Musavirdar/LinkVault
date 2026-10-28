using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum BackgroundType
{
    Solid,
    Gradient,
    Image,
    Video
}

public enum ButtonStyle
{
    Rounded,
    Sharp,
    Outline,
    Shadow,
    Pill,
    Glassmorphism
}

public enum ProfileLayout
{
    List,
    Grid,
    Carousel
}

public enum SocialLinksPosition
{
    Top,
    Bottom,
    Hidden
}

public class ProfileTheme
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Background
    [Required]
    public BackgroundType BackgroundType { get; set; } = BackgroundType.Gradient;
    
    /// <summary>
    /// Depends on BackgroundType:
    /// Solid → hex color (#1a1a2e),
    /// Gradient → CSS gradient (linear-gradient(135deg, #1a1a2e, #16213e)),
    /// Image → URL string,
    /// Video → URL string
    /// </summary>
    [MaxLength(2000)]
    public string BackgroundValue { get; set; } = "linear-gradient(135deg, #0f0c29, #302b63, #24243e)";
    
    // Button styling
    [Required]
    public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Rounded;
    
    [MaxLength(7)]
    public string ButtonColor { get; set; } = "#6366F1";
    
    [MaxLength(7)]
    public string ButtonTextColor { get; set; } = "#FFFFFF";
    
    /// <summary>Button hover effect color. Defaults to a lighter shade of ButtonColor.</summary>
    [MaxLength(7)]
    public string? ButtonHoverColor { get; set; }
    
    // Typography
    [MaxLength(100)]
    public string FontFamily { get; set; } = "Inter";
    
    // Layout
    [Required]
    public ProfileLayout ProfileLayout { get; set; } = ProfileLayout.List;
    
    // Social links
    [Required]
    public SocialLinksPosition SocialLinksPosition { get; set; } = SocialLinksPosition.Bottom;
    
    // Custom CSS (premium feature)
    [MaxLength(5000)]
    public string? CustomCSS { get; set; }
    
    /// <summary>Whether to hide the "Powered by LinkVault" footer. Premium only.</summary>
    public bool HideBranding { get; set; } = false;
    
    // Foreign key
    [Required]
    public Guid UserId { get; set; }
    
    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
