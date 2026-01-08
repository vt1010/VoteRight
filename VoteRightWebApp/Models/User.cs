using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace VoteRightWebApp.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public int PhoneNumber { get; set; } 

    [Required]
    [MaxLength(20)]
    public int WhatsAppNumber { get; set; } 

    [Required]
    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PoliticalPartyOrganization { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OrganizationalPosition { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<Download>? Downloads { get; set; }
}
