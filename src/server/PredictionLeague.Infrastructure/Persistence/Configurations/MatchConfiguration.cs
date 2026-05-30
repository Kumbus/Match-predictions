using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PredictionLeague.Domain.Entities;

namespace PredictionLeague.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.HomeTeam).IsRequired().HasMaxLength(100);
        builder.Property(m => m.AwayTeam).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Status).HasConversion<int>();

        builder.HasMany(m => m.Events)
            .WithOne()
            .HasForeignKey(e => e.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
