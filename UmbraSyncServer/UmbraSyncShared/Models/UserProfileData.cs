using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MareSynchronosShared.Models;

public class UserProfileData
{
    public string Base64ProfileImage { get; set; }
    public bool FlaggedForReport { get; set; }
    public bool IsNSFW { get; set; }
    public bool ProfileDisabled { get; set; }
    public User User { get; set; }

    public string UserDescription { get; set; }
    public string? RpProfilePictureBase64 { get; set; }
    public string? RpDescription { get; set; }
    public bool IsRpNSFW { get; set; }
    public string? RpFirstName { get; set; }
    public string? RpLastName { get; set; }
    public string? RpTitle { get; set; }
    public string? RpAge { get; set; }
    public string? RpHeight { get; set; }
    public string? RpBuild { get; set; }
    public string? RpOccupation { get; set; }
    public string? RpAffiliation { get; set; }
    public string? RpAlignment { get; set; }
    public string? RpAdditionalInfo { get; set; }

    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
}