# Run and deploy TaskTrack

## Local development

1. Install .NET 8 SDK and PostgreSQL.
2. Use the existing `TaskManagementDB`. The SQL under `database/` is the original destructive initialization script, for a new disposable database only. Do not rerun it on an existing database.
3. Put `ConnectionStrings:TaskManagement` into `TaskTrack.API/appsettings.Development.json` (ignored by Git).
4. Run `dotnet tool restore` and `dotnet restore`.
5. In PowerShell, set `$env:ASPNETCORE_ENVIRONMENT="Development"` and `$env:DOTNET_ENVIRONMENT="Development"`, then run `dotnet run --project TaskTrack.API --no-launch-profile --urls http://localhost:5080`.
6. Open `http://localhost:5080/swagger` and `http://localhost:5080/health/ready`.

The application never calls EnsureCreated, Migrate or initialization SQL.

## Database First

Entities were reverse engineered from the user's existing local PostgreSQL database. EF may report that database collations could not be loaded on PostgreSQL 18; table and relationship mappings were generated successfully and verified through API integration tests.

Use a configuration reference rather than a literal secret when scaffolding:

```powershell
dotnet ef dbcontext scaffold 'Name=ConnectionStrings:TaskManagement' Npgsql.EntityFrameworkCore.PostgreSQL --project TaskTrack.Repo --startup-project TaskTrack.API --output-dir Models --context-dir Data --context TaskManagementContext --no-onconfiguring
```

Supply the connection via local configuration/environment. Preserve the Task entity alias and the partial mapping extension when regenerating. The extension fixes EF's default-value sentinel so priority Low (0) is persisted instead of replaced by database default Medium (1).

## Render

Create a Web Service from the backend repository using its Dockerfile. Set `DATABASE_URL`, `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_HTTP_PORTS=8080`, `Swagger__Enabled=true`, and `Cors__Origins__0=https://YOUR-FRONTEND.vercel.app`. The health path is `/health/ready`.

The cloud backend needs a cloud-accessible PostgreSQL instance; it cannot access a database at localhost on the developer's PC. Import the supplied schema into a new Render database or migrate the existing database deliberately. Do not use the destructive seed script to update an established cloud database.

## Frontend

Import the separate frontend repository into Vercel, choose Next.js, and set `NEXT_PUBLIC_API_URL` to the Render backend origin (without `/api`). Redeploy after changing it; this variable is embedded at build time. Update backend CORS to the exact Vercel production origin.

## Verification

- `dotnet build --configuration Release`
- `python tests/api_integration.py http://localhost:5081` against a disposable database only. The script creates test data and intentionally leaves soft-deleted records intact.
- Frontend: `npm ci`, `npm run typecheck`, `npm run build`, `npx playwright install chromium`, `npm run test:e2e` with frontend and API running locally.

## Current scope

24 assignment endpoints plus task trash, restore, status update, liveness and readiness. No authentication, in accordance with the baseline. Never use this public CRUD baseline for private production data.

Future database-dependent work: users, membership, assignment, comments, attachments, persistent notifications, historical reports and automations. Current board ordering is not persisted; only task status is persisted. Pagination and sorting in the current frontend operate on API list results.
