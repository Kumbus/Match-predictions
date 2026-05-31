using Microsoft.AspNetCore.Identity;

namespace PredictionLeague.Infrastructure.Identity;

// The persisted user (FR-001). Identity-backed, Guid-keyed so the domain's bare
// *UserId Guids (League.OrganizerUserId, LeagueMembership.UserId, Prediction.UserId)
// line up with ApplicationUser.Id. The OAuth subject lives in AspNetUserLogins (F-02).
public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public bool IsGlobalAdmin { get; set; }
}
