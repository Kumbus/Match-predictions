using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PredictionLeague.Domain.Entities;

namespace PredictionLeague.Infrastructure.Persistence.Configurations;

public class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
{
    public void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Player).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Type).HasConversion<int>();
    }
}
