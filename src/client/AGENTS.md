# Client Guidelines

React 19 + Vite SPA (TypeScript) for Prediction League member screens — standings and prediction submission. Self-contained; consumes the ASP.NET Core API over HTTP.

## Commands

- Dev: `npm run dev`
- Build: `npm run build` — runs `tsc -b` then `vite build`; **type errors fail the build**
- Lint: `npm run lint`
- Preview built output: `npm run preview`

No tests exist yet. Don't claim any pass.

## Conventions

- ESLint flat config in `eslint.config.js` (typescript-eslint + react-hooks + react-refresh).
- ESM only (`"type": "module"`). React 19.
- `dist/` is build output and gitignored — never commit it.

## State

Fresh Vite scaffold: `src/` is still the starter `App.tsx` / `main.tsx`. No router, no data-fetching layer, no API client yet — these are assembled by hand (the chosen Vite-React starter ships none). When wiring API calls, the backend dev URL is `http://localhost:5185`.
