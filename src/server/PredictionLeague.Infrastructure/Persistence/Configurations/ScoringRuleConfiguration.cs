using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PredictionLeague.Domain.Entities;

namespace PredictionLeague.Infrastructure.Persistence.Configurations;

public class ScoringRuleConfiguration : IEntityTypeConfiguration<ScoringRule>
{
    public void Configure(EntityTypeBuilder<ScoringRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Parameter).HasConversion<int>();

        // One rule per (league, parameter) — scoring config is data-driven, not duplicated.
        builder.HasIndex(r => new { r.LeagueId, r.Parameter }).IsUnique();
    }
}
