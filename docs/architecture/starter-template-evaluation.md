# Starter Template Evaluation

### Primary Technology Domain

Full-stack web application — backend and frontend scaffolded independently, co-located in a single monorepo root.

### Repository Structure Decision

A flat monorepo with two top-level directories:

```
BookTracker/
├── backend/    ← ASP.NET Core Web API project
├── frontend/   ← React + Vite + TypeScript project
└── README.md
```

Rationale: Keeps both halves in one repo (single clone, single README) without requiring a monorepo tool. Simple enough for a demo-scope project; no workspaces or Turborepo needed.

### Backend Starter: ASP.NET Core Web API — .NET 10

**Initialization Command:**

```bash
dotnet new webapi --use-controllers -n BookTracker.Api -o backend
```

`--use-controllers` is required — .NET 8+ defaults to Minimal APIs, which conflicts with the PRD's three-tier controller requirement (§5.3, Addendum §A).

**Required NuGet packages to add after scaffolding:**

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

**Architectural Decisions Provided by Starter:**

- **Language & Runtime:** C#, .NET 10 LTS (SDK 10.0.300, released May 2026)
- **Web Framework:** ASP.NET Core MVC with `[ApiController]` + `ControllerBase` — no view rendering, JSON responses only
- **Dependency Injection:** Built-in `IServiceCollection` container; all Services and Repositories registered here
- **Configuration:** `appsettings.json` + environment variable overrides (connection string + JWT secret injected at runtime per §5.4 Local Runnability)
- **Build Tooling:** `dotnet` CLI; `dotnet run` for local dev
- **Project Structure (established by convention):**
  ```
  backend/
  ├── Controllers/
  ├── Services/        ← interfaces + implementations
  ├── Repositories/    ← interfaces + implementations
  ├── Models/          ← domain entities
  ├── DTOs/
  ├── Data/            ← DbContext, migrations
  └── Program.cs
  ```

### Frontend Starter: Vite + React + TypeScript

**Initialization Commands:**

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
npm install tailwindcss @tailwindcss/vite
npm install @radix-ui/react-dialog @radix-ui/react-visually-hidden
```

**Architectural Decisions Provided by Starter:**

- **Language:** TypeScript (strict mode)
- **Framework:** React 19 (current at Vite scaffold time)
- **Build Tooling:** Vite 6 — native ESM dev server, Rolldown production build, HMR out of the box
- **Styling:** Tailwind CSS v4 via `@tailwindcss/vite` plugin. **Important difference from UX spec:** Tailwind v4 replaces `tailwind.config.js` token centralization with a CSS `@theme` block in the main stylesheet. All design tokens (colors, shadows, border-radius, font-family) live in `src/index.css` under `@theme { ... }` instead of a JS config file. All UX spec token decisions remain valid — only the location changes.
- **Accessibility primitives:** Radix UI `Dialog` (for ProgressPopup and Journal popup — focus trapping, ARIA roles) + `VisuallyHidden`. Zero visual output from Radix; all styling is custom Tailwind.
- **State Management:** React Context (per Brief/Addendum; confirmed in step 4)
- **Project Structure (established by scaffold):**
  ```
  frontend/
  ├── src/
  │   ├── components/
  │   ├── pages/
  │   ├── context/
  │   ├── hooks/
  │   ├── api/
  │   └── index.css    ← Tailwind @theme tokens live here
  ├── vite.config.ts
  └── package.json
  ```

**Note:** Project initialization should be the first two implementation stories: (1) backend scaffold + packages, (2) frontend scaffold + Tailwind + Radix setup.
