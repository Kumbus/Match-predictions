# Lessons Learned

> Append-only register of recurring rules and patterns. Re-read at start by /10x-frame, /10x-research, /10x-plan, /10x-plan-review, /10x-implement, /10x-impl-review.

## Custom string properties need explicit HasMaxLength

- **Context**: src/server/PredictionLeague.Infrastructure/Identity/ApplicationUser.cs:10 → migration nvarchar(max)
- **Problem**: DisplayName had no HasMaxLength, so EF materialized it as nvarchar(max). Plan called for max-lengths on lookup strings; every other string column complied — this one slipped because it lives on the Identity-derived ApplicationUser, outside the per-entity Fluent configs where the team set lengths.
- **Rule**: Every string property that EF maps — including custom props added to Identity base types — must get an explicit IsRequired()/HasMaxLength() in a Fluent IEntityTypeConfiguration. Never let a queryable/user-facing string default to nvarchar(max).
- **Applies to**: All EF Core entity + Identity configurations (src/server/PredictionLeague.Infrastructure/Persistence/Configurations/).
