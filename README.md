# TaskTrack backend

ASP.NET Core 8, PostgreSQL and EF Core Database First. The original five-table schema is preserved. Controllers delegate business rules to Services; Repositories own database access.

## Local configuration

Put a local connection string in `TaskTrack.API/appsettings.Development.json` under `ConnectionStrings:TaskManagement`. This file is ignored by Git. Never commit database credentials. Alternatively set `DATABASE_URL`.

StudentID and ClassCode in the solution name are placeholders pending the student's identifiers.

## Implementation sequence

1. Scaffold solution and configuration.
2. Reverse engineer the existing database and implement repositories.
3. Implement validated public API and deletion rules.
4. Verify API integration and edge cases.
5. Add schema-preserving extensions and deployment documentation.

No automatic database creation, migrations or seed reset runs on application startup.
