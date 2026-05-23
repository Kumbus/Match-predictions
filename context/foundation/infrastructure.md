---
project: Football Match Prediction App
researched_at: 2026-05-23
recommended_platform: Azure App Service
runner_up: Railway
context_type: mvp
tech_stack:
  language: C#
  framework: ASP.NET Core Web API (.NET 10) + React 19 / Vite SPA
  runtime: .NET 10 (LTS)
---

## Recommendation

**Deploy on Azure App Service.** It scored 5/5 against the agent-friendly criteria, the developer already knows Azure, and the stack was shaped for `azure-app-service` from the start. .NET 10 is natively supported (no Dockerfile required), and the cost concern — Azure's weakest axis under a cost-minimize priority — is resolved by a decoupled shape: SPA on Static Web Apps Free, API on App Service Free (F1), background work on Azure Functions Consumption, and Azure SQL Basic/serverless for data. This lands at ~$5/mo, well under the $13/mo cap, with a clean upgrade path to App Service B1 (~$13) if Free-tier cold starts become annoying.

## Platform Comparison

Hard filter applied before scoring: the stack needs a **persistent .NET 10 server runtime + scheduled background work**, which drops JS-only serverless/edge platforms (Cloudflare Workers, Vercel, Netlify) from the backend candidate pool. The four survivors all run always-on .NET containers/apps.

| Platform | CLI-first | Managed/Serverless | Agent-readable docs | Stable deploy API | MCP / Integration |
|---|---|---|---|---|---|
| **Azure App Service** | Pass | Pass | Pass | Pass | Pass |
| **Railway** | Pass | Pass | Pass | Pass | Partial |
| **Fly.io** | Pass | Partial | Pass | Pass | Pass |
| **Render** | Pass | Pass | Pass | Pass | Pass |

- **Azure App Service** — `az webapp` covers deploy / restart / log tail / slot-swap rollback, fully scriptable and non-interactive. PaaS-managed. Docs on learn.microsoft.com are markdown-backed (GitHub-sourced). Azure MCP Server is GA for resource management. Native .NET 10 runtime on Linux & Windows (GA, Ignite Nov 2025). Sole weak axis: cost — "Always On" (needed for in-process background jobs) requires Basic+ (~$13 Linux), which the decoupled shape sidesteps.
- **Railway** — Cheapest always-on managed option (~$5/mo incl. DB), one-click Postgres, clean agent docs with dedicated `/agents` + `/ai` sections. Knocks: no native .NET (Dockerfile mandatory), MCP server still work-in-progress (preview, checked 2026-05-23), and the developer has no prior Railway experience.
- **Fly.io** — Cheapest compute (~$2–3/mo always-on) and best global-later story (`fly regions add`), GA flyctl MCP (experimental flag). Knocks: container-only (you own the Dockerfile + autostop tuning so background jobs aren't suspended), and **Managed Postgres starts at $38/mo** — its co-located DB story is its weakest point.
- **Render** — Clean GA CLI, official MCP server, solid docs. Knocks: background recompute needs a **separate Background Worker (+$7/mo)** since the Free web service spins down after 15 min, and **free Postgres expires after 30 days** — more cost and lifecycle friction than the alternatives for no offsetting win. Dropped to 4th.

### Shortlisted Platforms

#### 1. Azure App Service (Recommended)

5/5 on the criteria, native .NET 10, developer familiarity (a strong real-world tie-breaker for a solo after-hours build), and a documented cheap path that meets the budget. The Vite SPA goes on Static Web Apps Free; the API on F1 Free; ingest + recompute on Azure Functions Consumption; data on Azure SQL Basic/serverless.

#### 2. Railway

Would win on pure cost + DX if familiarity weren't a factor — fully managed, ~$5/mo all-in, one-click Postgres. The gap vs. Azure: zero prior experience, Dockerfile required for .NET, and MCP still in preview. Strongest fallback if Azure's Free-tier limits (60 CPU-min/day, cold start) prove too tight and the B1 upgrade pushes past budget.

#### 3. Fly.io

Best if the app goes genuinely global later and compute cost is paramount. Pairing with an external free Postgres (Neon/Supabase) neutralizes its $38/mo managed-DB weakness. The gap: container/autostop tuning overhead and no Azure-style familiarity.

## Anti-Bias Cross-Check: Azure App Service

### Devil's Advocate — Weaknesses

1. **"Always On" is Basic+ only (~$13/mo Linux).** Any *in-process* `IHostedService` background recompute forces this, blowing the cost priority. Mitigated only by decoupling jobs to Azure Functions (the recommended shape).
2. **Single small instance has no job/request isolation.** A CPU-heavy scoring recompute running inside the web app can starve HTTP handling — another reason to keep recompute in Functions, not the web tier.
3. **Windows hosting is a ~4× cost trap** (~$55 vs ~$13 B1 for identical specs). Always select **Linux**.
4. **Azure SQL serverless auto-pause = cold-start latency.** First query after idle waits seconds for resume — hits exactly during the post-match recompute when results land.
5. **"Easy MCP" (App-Service-as-MCP-host) is preview** (checked 2026-05-23). Use the **GA Azure MCP Server** for resource management; don't build operability on the preview feature.

### Pre-Mortem — How This Could Fail

The solo dev ships on Azure because it's familiar. F1 Free looked attractive, so the API launched there — and an in-process recompute silently stopped firing whenever the friend group went quiet for 20 minutes, producing stale standings during the World Cup match-day spike (the product's one guardrail: scoring freshness). Switching to B1 fixed idling but the in-process recompute, sharing one small instance with request traffic, timed out under match-day load. The "cheap" Azure SQL serverless auto-paused overnight, so the morning-after recompute ate cold-start latency. Costs crept: B1 + SQL + egress drifted past the ~$13 budget toward $30+ once a deployment slot was added for safe releases. None of it was individually catastrophic, but after-hours weekends went to tuning Azure knobs (Always On, slot swaps, SQL compute floors) instead of building scoring features — the familiarity that justified the choice got eaten by configuration overhead the cheaper managed PaaS options would have hidden.

### Unknown Unknowns

- **.NET 10 on App Service Linux ships via the platform stack** — verify live with `az webapp list-runtimes --os-type linux` before assuming it's available in your target region; Windows updates on a different cadence.
- **The SPA does not belong on App Service** — Static Web Apps Free ($0, global CDN, GitHub Actions CI) is its correct home (cap: 10 SWAs/subscription, no SLA). Putting it on App Service wastes compute.
- **Deployment slots (blue-green rollback) require Standard tier**, not Basic — B1 has no slots. Safe zero-downtime rollback either costs more (S1 ~$70/mo) or you accept restart-in-place deploys.
- **Azure SQL serverless still bills when "paused"** — storage + a compute floor accrue; the "~$5/mo" estimate assumes genuinely low active-hours. Azure SQL **Basic DTU** (~$4.90/mo flat, 5 DTU / 2GB) is the predictable-cost alternative with no cold start.
- **F1 Free has a 60 CPU-min/day/region cap** — burning it returns HTTP 403 ("quota exceeded"). Acceptable at friend-group scale but a hard ceiling to watch on match days.

## Operational Story

- **Preview deploys**: App Service deployment slots give staging URLs, but **slots require Standard tier** — on Free/Basic there are no slots. MVP approach: deploy to a separate `-staging` App Service (also Free) via GitHub Actions on PR, promote by deploying to prod on merge. The SPA gets automatic per-PR preview URLs from Static Web Apps Free (GitHub Actions–driven).
- **Secrets**: App settings / connection strings live in **App Service Configuration** (or Key Vault references) and **GitHub Actions Secrets** for the CI pipeline — never committed. Function secrets live in the Function App's Configuration. DB connection string injected as an app setting, read by EF Core at startup. Rotate via `az webapp config appsettings set` + restart.
- **Rollback**: On Free/Basic (no slots), redeploy the previous artifact: `az webapp deploy` with the prior build, or re-run the previous successful GitHub Actions run. Time-to-revert ≈ one deploy (~1–2 min). Caveat: EF Core migrations do **not** auto-roll-back — a forward-only migration must be reversed by a new migration, never by reverting code alone.
- **Approval**: Agent may deploy to staging and tail logs unattended. **Human-only**: publishing to production, rotating the DB connection string / primary secrets, dropping or migrating the database, deleting the App Service or Function App. Manual click = 30s; cleanup after an automated mistake = hours.
- **Logs**: `az webapp log tail --name <app> --resource-group <rg>` for live API logs (`--slot` if slots exist later); Functions logs via `az webapp log tail` on the Function App or Application Insights live metrics. GitHub Actions run logs via `gh run view`. Azure MCP Server (GA) exposes structured resource/log queries if the agent needs many of them.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| In-process background recompute requires Always On (Basic+), breaks budget | Devil's advocate | H | M | Decouple ingest + recompute to Azure Functions Consumption (timer trigger); keep web tier on F1 Free |
| F1 Free idles → stale standings during match-day spike | Pre-mortem | M | H | Run jobs on Functions (fire regardless of web-app state); upgrade API to B1 if request-tier cold starts hurt |
| Recompute starves HTTP on a single small instance | Devil's advocate | M | M | Keep recompute out of the web app (Functions); never co-locate CPU-heavy work with request handling at MVP tier |
| Accidentally provisioning Windows hosting (~4× cost) | Devil's advocate | L | M | Always specify `--os-type linux`; assert in deploy plan |
| Azure SQL serverless cold-start latency on post-match recompute | Devil's advocate / Unknown unknowns | M | M | Use Azure SQL **Basic DTU** (~$5 flat, no auto-pause) for predictable latency; or set a higher serverless auto-pause delay |
| F1 60 CPU-min/day cap → 403 on heavy match day | Unknown unknowns | M | M | Monitor CPU quota; upgrade API to B1 (~$13, still in budget) before a major tournament |
| Deployment slots need Standard tier — no cheap blue-green | Unknown unknowns | M | L | Use a separate `-staging` Free App Service + GitHub Actions promotion instead of slots |
| .NET 10 Linux runtime not yet in target region | Unknown unknowns | L | M | Verify `az webapp list-runtimes --os-type linux` before first deploy |
| EF Core migration cannot auto-roll-back with code revert | Research finding | M | H | Treat migrations as forward-only; write a reversing migration; gate prod migrations behind human approval |
| Cost creep past $13 once slots/extra services added | Pre-mortem | M | M | Keep the decoupled Free-tier shape; review monthly billing; B1 upgrade is a deliberate decision, not a default |

## Getting Started

Versions assume .NET 10 SDK installed locally and the Azure CLI. Validate the runtime is live in your region before deploying.

1. **Install/confirm tooling**: `az --version` (Azure CLI) and `func --version` (Azure Functions Core Tools, v4 for .NET 10 isolated worker). Login: `az login`.
2. **Verify .NET 10 availability** in your target region: `az webapp list-runtimes --os-type linux | findstr -i dotnet`.
3. **Create the API (Linux, Free tier)**: `az appservice plan create -g <rg> -n <plan> --sku F1 --is-linux` then `az webapp create -g <rg> -p <plan> -n <api-app> --runtime "DOTNETCORE:10.0"`.
4. **Deploy the API**: from `server/`, `dotnet publish -c Release -o ./publish`, zip it, then `az webapp deploy -g <rg> -n <api-app> --src-path publish.zip --type zip`.
5. **Background jobs as a Function** (decoupled, keeps API off Always On): scaffold a .NET 10 isolated Timer-triggered Function for data ingest + post-match recompute; deploy on the **Consumption** plan (`az functionapp create ... --consumption-plan-location <region>`).
6. **SPA on Static Web Apps Free**: `az staticwebapp create -g <rg> -n <spa> --source <repo> --branch main --app-location client --output-location dist` — wires GitHub Actions CI + per-PR previews automatically.
7. **Database**: create Azure SQL **Basic** (predictable ~$5/mo, no cold start) — `az sql server create` + `az sql db create ... --service-objective Basic`. Add the connection string as an App Service app setting; point EF Core (SQL Server provider) at it and run migrations.

## Out of Scope

The following were not evaluated in this research:
- Docker image configuration / Dockerfiles
- CI/CD pipeline setup (GitHub Actions workflow authoring)
- Production-scale architecture (multi-region, HA, DR)
