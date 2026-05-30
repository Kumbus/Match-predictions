using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PredictionLeague.Domain.Entities;

namespace PredictionLeague.Infrastructure.Persistence.Configurations;

public class LeagueMembershipConfiguration : IEntityTypeConfiguration<LeagueMembership>
{
    public void Configure(EntityTypeBuilder<LeagueMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).HasConversion<int>();

        // One membership per (user, league).
        builder.HasIndex(m => new { m.LeagueId, m.UserId }).IsUnique();

        // UserId is a bare Guid → ApplicationUser.Id; no FK constraint this slice.
    }
}
