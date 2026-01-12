using System.ComponentModel.DataAnnotations;

namespace VoteRightWebApp.Models;

public class FileDownloadEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Assembly { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Booths { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; }

    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}
