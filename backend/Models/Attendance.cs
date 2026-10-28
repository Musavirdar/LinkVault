using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum AttendanceAction
{
    Upload,
    Link,
    Chat,
    Login,
    Directory
}

public class Attendance
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    /// <summary>The calendar day this activity belongs to (UTC date).</summary>
    [Required]
    public DateOnly Date { get; set; }

    /// <summary>The total number of qualifying actions performed on this day.</summary>
    public int Count { get; set; } = 1;

    /// <summary>Primary action type that drove the first activity of this day.</summary>
    public AttendanceAction Action { get; set; } = AttendanceAction.Login;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
