---
project: Football Match Prediction App
updated: 2026-05-20
context_type: greenfield
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  frs_drafted: 12
  quality_check_status: accepted
product_type: web-app
target_scale: medium
timeline_budget:
  mvp_weeks: 3
  hard_deadline: 2026-06-11
  after_hours_only: true
---

# Shape Notes — Football Match Prediction App

(seed: idea-notes.md — private prediction leagues with custom scoring, no money)

## Vision & Problem Statement

Free football match-prediction games exist today only for the biggest tournaments
(World Cup, Euro) and only on the official organizer sites. Those games are rigidly
managed by the organizer: the scoring rules are fixed and cannot be tailored to a
group's own preferences.

The core pain is a **missing capability** — there is no way to run a private,
no-money prediction pool with *custom scoring rules* of your own design.

**Insight:** scoring is personal and social. Each friend group wants its own
rules; a single fixed scoring scheme kills the fun. The product's value is letting
a group define what counts and how many points it's worth.

## User & Persona

**Primary persona:** the *league organizer* — a person who sets up a prediction
pool for a given tournament, defines the scoring rules, and invites their friend
group to play.

## Access Control

Users sign in via **OAuth / social login** (e.g. Google). Bets and league
membership are tied to the account, enabling different predictions per league
under one identity.

Three roles:
- **Global admin** — adds tournaments and their matches (data source of truth).
- **League organizer** — creates a league for a tournament, defines its scoring
  rules, invites members.
- **League member** — joins via invite, submits predictions, sees standings.

## Success Criteria

### Primary
The full loop works without manual result entry: organizer creates a league for
an admin-seeded tournament, sets custom scoring rules, invites friends; members
predict upcoming matches; results are pulled automatically from a football data
API and predictions are scored per the league's rules; standings update.

### Secondary
Match detail is rich enough for scoring on granular events — who played, final
score, goal scorers, yellow/red cards — sourced from the API.

### Guardrails
- **Scoring correctness** — predictions scored exactly per each league's defined
  rules; wrong standings kills the product.
- **Prediction lock at kickoff** — no edits/new predictions after a match starts.
- **Result accuracy & freshness** — API results correct and reflected within a
  sane window after a match ends.

## Timeline acknowledgment
Acknowledged on 2026-05-20: hard deadline 2026-06-11 (World Cup kickoff). ~3-week
window from today. MVP requires sustained dedication; user accepted. Live API result ingestion is in v1 by design — a manual-entry admin
module (per-match goals, scorers, cards) would be *more* work than consuming an
API that already supplies that data, so the API is the lean path, not a luxury.

## Functional Requirements

### Accounts & Access
- FR-001: User can sign in via OAuth/social login. Priority: must-have
  > Socrates: Counter considered: "OAuth ties us to a provider; some friends may avoid Google/FB." Resolution: kept for v1; revisit allowing email/passwordless as alternative if a member can't use the chosen provider (see Open Questions).
- FR-002: User can submit different predictions in different leagues under one account. Priority: must-have
  > Socrates: Counter considered: "premature for v1 if most users join one league." Resolution: kept — cross-league identity is the stated insight; data model keys bets per (user, league) from the start to avoid rework.

### Tournaments & Match Data (admin / API)
- FR-003: Admin can add a tournament. Priority: must-have
  > Socrates: Counter considered: "admin-add makes you a bottleneck on every new tournament." Resolution: kept — manual admin gatekeeping protects data quality for v1; self-serve tournament creation is explicitly a non-goal.
- FR-004: System ingests a tournament's fixtures & results from a football API. Priority: must-have
  > Socrates: Counter considered: "API cost/limits/coverage risk could block the MVP." Resolution: kept (it is the success criterion), but API selection is now a load-bearing Open Question — must confirm a source covering fixtures + results + scorers + cards within budget before build.
- FR-005: System records per-match detail: teams, final score, goal scorers, yellow/red cards. Priority: must-have
  > Socrates: Counter considered: "scorer/card detail is often a paid API tier." Resolution: kept as target; depends on FR-004 source. Fallback: if detail unavailable, v1 scoring degrades to final-score-based rules (see Open Questions).

### Leagues
- FR-006: Organizer can create a league tied to a tournament. Priority: must-have
  > Socrates: Counter considered: "groups may want a season-long pool spanning tournaments." Resolution: kept one-tournament-per-league for v1 scoping; multi-tournament / season leagues noted as a v2 candidate.
- FR-007: Organizer can invite friends to a league. Priority: must-have
  > Socrates: No counter-argument; inviting friends is the social core. Stands as written.
- FR-008: Organizer can define custom scoring rules — which match parameters score and how many points each. Priority: must-have
  > Socrates: No counter-argument; custom scoring is the core insight. Stands as written. (Correctness/validation risk tracked via FR-011 + Open Questions.)

### Predictions & Scoring
- FR-009: Member can submit predictions for upcoming matches. Priority: must-have
  > Socrates: No counter-argument; core member action. Stands as written.
- FR-010: System locks predictions at match kickoff. Priority: must-have
  > Socrates: No counter-argument; anti-cheat guardrail. Stands as written. (Depends on accurate, timezone-safe kickoff times from the API.)
- FR-011: System scores predictions per the league's rules after results arrive. Priority: must-have
  > Socrates: Counter considered: "scoring correctness is only as good as the custom-rule engine (coupled to FR-008)." Resolution: kept; the rule engine + scoring are the riskiest correctness surface — needs explicit validation strategy (see Open Questions).
- FR-012: Member can view standings/table and upcoming + past matches. Priority: must-have
  > Socrates: No counter-argument; without visible standings the pool has no point. Stands as written.

## Quality cross-check
All elements present (accepted): Access Control, Business Logic (one-sentence rule), project artifacts, timeline-cost acknowledgment, Non-Goals. No gaps. Open decisions are captured in `## Open Questions` for /10x-prd to mirror.

## Business Logic

The app computes each player's score by comparing their match predictions against
the actual match outcome, applying the league's custom-defined scoring rules.

Inputs the rule consumes (user-facing): a player's predictions for a match, the
match's actual outcome (score and, where available, granular events such as goal
scorers and cards), and the league's scoring configuration (which parameters
count and how many points each). Output: per-player points for the match, which
aggregate into the league standings. The user encounters it as their points and
position updating automatically after a match completes — they never enter
results or compute scores by hand.

## Non-Functional Requirements

- Results and recomputed standings become visible to members within a short,
  user-perceived window after a match's final whistle (target: minutes, not
  hours).
- Once a match has kicked off, it is observably impossible to submit or edit a
  prediction for that match.

## Non-Goals
- **User-created tournaments** — only the global admin adds tournaments in v1; self-serve tournament creation is out.
- **Sports other than football** — football only.
- **Social-media sharing** — no share-to-socials in v1.
- **Native mobile app** — responsive web only; no iOS/Android app.

## Open Questions
- **Hard deadline** — pinned: 2026-06-11 (World Cup kickoff). ~3 weeks from shaping date. Drives aggressive MVP scope.
- **Football data API selection** — must confirm a source covering fixtures, results, goal scorers, and cards within budget/rate limits before build. Blocks FR-004/FR-005. (downstream: tech-stack selection)
- **Granular-detail fallback** — if scorer/card data is unavailable at the chosen API tier, v1 scoring degrades to final-score-based rules. Confirm acceptable.
- **Scoring-rule validation** — how to guarantee the custom-rule engine (FR-008) scores correctly across arbitrary configs (test fixtures, recompute on late API corrections).
- **Auth provider coverage** — allow an alternative sign-in if a member lacks the chosen OAuth provider?

## User Stories

### US-01: Run a private prediction league end-to-end
- **Given** I am signed in and an admin has seeded a tournament,
- **When** I create a league for that tournament, define custom scoring rules, and invite friends who join and submit predictions before kickoff,
- **Then** after each match the system ingests the result, scores every prediction per my league's rules, and updates the standings — with no manual result entry.

