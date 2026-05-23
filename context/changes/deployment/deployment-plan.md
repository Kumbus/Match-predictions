# First Deployment — Prediction League (Azure)

## Context

First production deploy of the Prediction League MVP, per `context/foundation/infrastructure-v2.md` (Azure App Service recommendation) and `context/foundation/tech-stack.md` (.NET 10 API + React 19/Vite SPA).

The codebase is still bare scaffold: the API uses a `static List<League>` placeholder (no EF/data layer), background-job code does not exist, and auth is declared but unwired. Therefore **this deploy provisions only the two services that have shippable code**:

- **API** → Azure App Service, **F1 Free**, Linux, region **Poland Central**
- **SPA** → Azure **Static Web Apps Free** (origin in **West Europe** — Poland Central is not a SWA build region; CDN is global regardless)

**Deferred** (no code yet, revisit when EF + job code land): Azure SQL Basic, Azure Functions (ingest + post-match recompute). This keeps first deploy at ~$0/mo and avoids empty shells.

### Decided
- Scope: API + SPA only.
- API region: Poland Central. SPA origin: West Europe.

### Known constraints carried from infra-v2
- F1 Free: 60 CPU-min/day/region cap → HTTP 403 on overage. Fine at friend scale; watch on match days.
- No deployment slots on Free → rollback = redeploy prior artifact.
- `func` Core Tools and `gh` are NOT installed — not needed for this scope (SWA auth uses `az ... --login-with-github` device flow).

### Naming (proposed)
| Resource | Name | Region |
|---|---|---|
| Resource group | `rg-prediction-league` | polandcentral |
| App Service plan | `asp-prediction-league` (F1, `--is-linux`) | polandcentral |
| Web app (API) | `prediction-league-api-0523444a` *(base name was taken; suffix added)* | polandcentral |
| Static Web App (SPA) | `prediction-league-web` | westeurope |

### Live URLs (deployed 2026-05-23)
- **API:** https://prediction-league-api-0523444a.azurewebsites.net — `GET /api/leagues` → `200 []` ✅
- **SPA:** _(pending Phase 4)_
- Subscription: `dd204810-4214-41a4-880b-05fe99e649e4` (Visual Studio Enterprise – MPN)

> Commands are written for **PowerShell** (use backtick `` ` `` for line continuation, not `\`). Linux/bash equivalents differ only in line-continuation.

---

# Part 1 — Execution checklist (the actual deploy run)

## Phase 0 — Preflight & verification (read-only + human login)

- [ ] **HUMAN:** `az login` (interactive — run via `! az login` in the prompt). Agent cannot do this.
- [ ] `az account show` — confirm correct subscription/tenant.
- [ ] Verify .NET 10 runtime exists: `az webapp list-runtimes --os-type linux | findstr -i dotnet` → expect `DOTNETCORE:10.0`.
- [ ] Confirm Poland Central allows F1 Linux plan creation (caught in Phase 2; fallback **West Europe** if it errors).
- [ ] Confirm name `prediction-league-api` is free: `az webapp list -o table` / rely on create-time uniqueness error.

## Phase 1 — Build artifacts locally (agent)

- [x] API: `dotnet publish src/server/PredictionLeague.csproj -c Release -o publish` ✅
- [x] Zip publish output → `publish.zip` (372 KB) ✅
- [x] SPA build smoke test: `npm ci` + `npm run build` → `dist/` built clean (vite 8.0.14, 193 KB JS) ✅

## Phase 2 — Provision API (agent)

- [x] `az group create` → polandcentral ✅
- [x] `az appservice plan create --sku F1 --is-linux` ✅
- [x] `az webapp create ... -n prediction-league-api-0523444a --runtime "DOTNETCORE:10.0"` ✅ (base name globally taken → suffix added)
- [x] polandcentral supported F1 Linux + .NET 10 — no fallback needed ✅

## Phase 3 — Deploy + smoke-test API (agent)

- [x] `az webapp deploy --type zip` → `RuntimeSuccessful` ✅
- [x] No startup override needed — .NET 10 auto-detected ✅
- [x] Smoke test: `GET /api/leagues` → `200 []` ✅

## Phase 4 — Provision + deploy SPA (agent + human GitHub auth)

- [ ] **HUMAN:** run the `az staticwebapp create` command (GitHub device-code auth — interactive). See §D.
- [ ] This commits a GitHub Actions workflow to the repo and triggers the first SPA build/deploy. **Remote-visible** — confirm before running.
- [ ] Verify SPA: open the SWA default hostname → starter Vite page loads over global CDN.

## Phase 5 — Wiring & follow-ups (note only, not blocking first deploy)

- [ ] **CORS:** `Program.cs` configures no CORS. Not needed yet — starter `App.tsx` makes no API calls. Add an allowed-origin CORS policy when the SPA first calls the API.
- [ ] **API base URL:** inject SWA→API URL via a `VITE_` env when the data layer is built.
- [ ] **HTTPS-only:** `az webapp update --set httpsOnly=true` (optional hardening).
- [ ] Defer Azure SQL Basic + Functions to a later deploy once EF/job code exists.

---

# Part 2 — Detailed component setup reference

## §A. Local toolchain

Already verified on this machine: **.NET SDK 10.0.300**, **Node v22.16.0**, **Azure CLI 2.86.0**. The rest of §A is for a clean machine or teammate.

| Tool | Check | Install (Windows / winget) | Notes |
|---|---|---|---|
| .NET 10 SDK | `dotnet --version` → `10.x` | `winget install Microsoft.DotNet.SDK.10` | Needed for `dotnet publish`. |
| Node.js 20+ | `node --version` | `winget install OpenJS.NodeJS.LTS` | Vite 8 + React 19 build. |
| Azure CLI | `az version` | `winget install Microsoft.AzureCLI` | Keep current: `az upgrade`. |
| func Core Tools v4 | `func --version` | `npm i -g azure-functions-core-tools@4` | **Deferred** — only when Functions code lands. |
| gh CLI | `gh --version` | `winget install GitHub.cli` | Optional — SWA uses `--login-with-github` instead. |

After install, restart the shell so PATH refreshes.

## §B. Azure account & subscription

1. Need an active Azure subscription (free tier works; F1 App Service + SWA Free cost $0).
2. **HUMAN:** `az login` → opens browser / device code. Headless: `az login --use-device-code`.
3. Pick the subscription:
   ```powershell
   az account list -o table
   az account set --subscription "<SUBSCRIPTION_NAME_OR_ID>"
   az account show -o table   # confirm
   ```
4. Register the resource providers (one-time per subscription; no-op if already registered):
   ```powershell
   az provider register --namespace Microsoft.Web        # App Service + Static Web Apps
   ```
   (`Microsoft.Sql` is deferred with the database.)

## §C. App Service (the API) — full setup

**What it is:** PaaS host for the .NET 10 Web API on a Free (F1) Linux plan, single region (Poland Central).

1. **Resource group** (logical container):
   ```powershell
   az group create -n rg-prediction-league -l polandcentral
   ```
2. **App Service plan** — the compute. `F1` = Free, `--is-linux` mandatory (Linux is ~4× cheaper than Windows at paid tiers and the infra doc mandates it):
   ```powershell
   az appservice plan create -g rg-prediction-league -n asp-prediction-league `
     --sku F1 --is-linux -l polandcentral
   ```
3. **Web app** — bound to the plan, .NET 10 runtime:
   ```powershell
   az webapp create -g rg-prediction-league -p asp-prediction-league `
     -n prediction-league-api --runtime "DOTNETCORE:10.0"
   ```
   Name must be **globally unique** (becomes `prediction-league-api.azurewebsites.net`). On collision, append a suffix.
4. **App settings** (environment): production is the default `ASPNETCORE_ENVIRONMENT`. No connection string yet (no DB). To set any later:
   ```powershell
   az webapp config appsettings set -g rg-prediction-league -n prediction-league-api `
     --settings KEY=VALUE
   ```
   Secrets live here (or Key Vault refs) — **never** committed to the repo.
5. **Build & package locally**, then zip-deploy:
   ```powershell
   dotnet publish src/server/PredictionLeague.csproj -c Release -o publish
   Compress-Archive -Path publish/* -DestinationPath publish.zip -Force
   az webapp deploy -g rg-prediction-league -n prediction-league-api `
     --src-path publish.zip --type zip
   ```
6. **Startup (only if boot fails):** .NET on Linux App Service usually auto-detects the entry DLL and the platform injects the listening port. If it 502s on first hit:
   ```powershell
   az webapp config set -g rg-prediction-league -n prediction-league-api `
     --startup-file "dotnet PredictionLeague.dll"
   ```
7. **Logs / diagnose:**
   ```powershell
   az webapp log tail -g rg-prediction-league -n prediction-league-api
   ```
8. **Rollback (no slots on Free):** rebuild the previous commit and re-`az webapp deploy` the prior zip. Time-to-revert ≈ one deploy.

## §D. Static Web Apps (the SPA) — full setup

**What it is:** Free global-CDN host for the Vite build, with GitHub Actions CI wired automatically. Origin/build region must be a SWA-supported region → **West Europe** (Poland Central is not one); the CDN is global regardless, so end-user latency is unaffected.

1. **Create + link GitHub** (interactive device-code auth — **HUMAN**):
   ```powershell
   az staticwebapp create -g rg-prediction-league -n prediction-league-web `
     -l westeurope `
     --source https://github.com/Kumbus/Repo `
     --branch features/lesson-4 `
     --app-location "src/client" `
     --output-location "dist" `
     --login-with-github
   ```
   - `--app-location` = where the SPA's `package.json` lives (monorepo subdir).
   - `--output-location` = build output dir relative to app-location (`vite build` → `dist`).
   - No `--api-location` (the .NET API is hosted separately on App Service, not as SWA managed functions).
2. **What this does:** SWA pushes a workflow to `.github/workflows/azure-static-web-apps-*.yml` on the chosen branch and stores the deploy token as a repo secret. Oryx in CI runs `npm install` + `npm run build` and publishes `dist/`. Every push to the branch (and PRs → preview URLs) redeploys.
   - **Node version in CI:** SWA's Oryx picks a default Node. If the build needs Node 20/22, pin it via an `engines.node` field in `src/client/package.json` or an env var in the workflow.
3. **Verify:**
   ```powershell
   az staticwebapp show -g rg-prediction-league -n prediction-league-web `
     --query defaultHostname -o tsv
   ```
   Open the returned hostname → starter Vite page over HTTPS/CDN. Check the Actions run is green.
4. **Later — point SPA at the API:** add `VITE_API_BASE_URL=https://prediction-league-api.azurewebsites.net` as a build-time env (workflow or `staticwebapp.config.json`) once the client makes API calls; then add a matching CORS policy on the API (§Phase 5).

## §E. GitHub prerequisites

- Repo: `https://github.com/Kumbus/Repo`, deploying branch `features/lesson-4`.
- `az staticwebapp create --login-with-github` needs an account with **write** access (to push the workflow + set the secret). The device-code flow grants this without installing `gh`.
- The committed workflow + repo secret are the only repo-side artifacts; nothing sensitive is stored in the working tree.

---

## Verification (end-to-end)

1. API: `curl https://prediction-league-api.azurewebsites.net/api/leagues` returns 200 JSON.
2. SPA: SWA default hostname serves the built Vite app (HTTP 200, CDN headers).
3. GitHub Actions run for the SWA deploy is green.
4. Record final URLs + chosen region in this file as the deploy audit trail.

## Human-only gates (per infra-v2 operational story)
- `az login`, GitHub auth, and the SWA create (pushes workflow to repo) are human-confirmed.
- No DB to drop / no primary secret to rotate in this scope.
