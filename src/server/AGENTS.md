# Server Guidelines

ASP.NET Core Web API (.NET 10, C#) for Prediction League. This directory is self-contained — the React client lives elsewhere and talks to this over HTTP.

## Commands

- Run: `dotnet run` (dev URL `http://localhost:5185`; sample requests in `PredictionLeague.http`)
- Build: `dotnet build`
- Solution: `prediction-league.slnx` — `.slnx` XML format, not `.sln`

No tests exist yet. Don't claim any pass.

## Conventions

- Nullable reference types and implicit usings are **on** (`PredictionLeague.csproj`). Mark non-nullable model props `required`.
- Single namespace root `PredictionLeague.*`; controllers use `[ApiController]` + `[Route("api/[controller]")]`.
- Model files (`Models/`) carry `// FR-00x` comments tying types to PRD requirements — keep them when editing.

## Traps

- **In-memory placeholder store.** `Controllers/LeaguesController.cs` keeps a `static List<League>`. This is a stand-in, not the data layer — Entity Framework is the plan. Swap it when persistence lands; don't build features on the static store.
- **Auth declared, not wired.** `Program.cs` calls `UseAuthorization()` but nothing configures it. ASP.NET Core Identity + OAuth is planned.

## Domain

Scoring is per-league and data-driven: `ScoringRule` maps a `ScoringParameter` (`ExactScore`, `CorrectOutcome`, `CorrectGoalScorer`, `CorrectCardCount`) to `Points`. Never hardcode point values.
