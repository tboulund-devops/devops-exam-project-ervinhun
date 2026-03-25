# Copilot Instructions

## Project Overview

**To-do with extras** is a task management REST API built with ASP.NET Core. Tasks move through workflow states — To-Do, Doing, Review, and Done — similar to a lightweight Kanban board. The API exposes endpoints for CRUD operations on tasks, status transitions, and related features (comments, notifications, user assignment, etc.).

## Tech Stack

- **Backend:** ASP.NET Core Web API (.NET 10), C#
- **ORM:** Entity Framework Core
- **Database:** PostgreSQL
- **API Docs:** Swagger / OpenAPI
- **Containerization:** Docker + Docker Compose
- **Testing:** xUnit with Testcontainers (PostgreSQL)

## Repository Structure

```
.
├── .github/
│   ├── copilot-instructions.md   # This file
│   └── CODEOWNERS
├── server/                       # ASP.NET Core Web API project
│   ├── Controller/               # API controllers
│   ├── DataAccess/               # EF Core DbContext, schema.sql
│   ├── Models/                   # EF Core entity models
│   ├── Utils/                    # Helpers (DatabaseSeeder, SaveTaskToHistory, etc.)
│   ├── Program.cs                # App entry point and DI setup
│   ├── appsettings.json
│   └── server.csproj
└── test/                         # Integration test project (xUnit + Testcontainers)
    ├── CustomWebApplicationFactory.cs
    ├── TestDataSeeder.cs
    └── test.csproj
```

## Build & Run

```bash
# Restore and build
dotnet build server/server.csproj

# Run locally (requires a running PostgreSQL instance and CONNECTION_STRING env var)
CONNECTION_STRING="Host=localhost;Database=tododb;Username=postgres;Password=secret" \
  dotnet run --project server/

# Run with Docker Compose (API + DB together)
docker compose up --build
```

## Environment Variables

| Variable            | Required       | Description                          |
|---------------------|----------------|--------------------------------------|
| `CONNECTION_STRING` | Yes (non-Test) | PostgreSQL connection string         |
| `ASPNETCORE_ENVIRONMENT` | No      | Defaults to `Production`; use `Development` locally or `Test` for integration tests |

> **Note:** When `ASPNETCORE_ENVIRONMENT` is `Test`, `Program.cs` skips `DbContext` registration and `DatabaseSeeder` initialization. Integration tests register the `DbContext` themselves via `CustomWebApplicationFactory`.

## Testing

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

Integration tests use **Testcontainers** to spin up a real PostgreSQL container (`postgres:16-alpine`). The `CustomWebApplicationFactory` applies `DataAccess/schema.sql` and seeds base data via `TestDataSeeder.SeedBaseData()`.

Key testing conventions:
- `TestDataSeeder` only seeds users and a `"Backlog"` status by default. Create other statuses (e.g., `"To-do"`, `"Done"`) per test using `EnsureStatus` to avoid order dependence.
- Tests do **not** reset DB state between runs — design tests to be independent or tolerant of existing data.

## Coding Conventions

- **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`). Always annotate nullability correctly.
- **Implicit usings** are enabled — no need to add common `using` statements manually.
- Follow standard **ASP.NET Core** patterns: constructor injection for dependencies, use `ILogger<T>` for logging.
- Keep controllers thin — delegate logic to service classes or utilities in `Utils/`.
- **Database schema** is defined in `server/DataAccess/schema.sql` (copied to output directory). `DatabaseSeeder` runs it at startup in non-Test environments.
- The `set_updated_at()` trigger in `schema.sql` keeps `updated_at` current on `UPDATE` for `users` and `task_item` tables — do not manually update `updated_at` in application code.
- Task history is recorded in `SaveTaskToHistory` for changes to `Title`, `Description`, and `AssigneeId`.
- When authentication is not yet implemented, use the `system` user (username `"system"`) for history records.

## API Conventions

- Use RESTful resource naming: `GET /tasks`, `POST /tasks`, `GET /tasks/{id}`, `PUT /tasks/{id}`, `DELETE /tasks/{id}`.
- Return appropriate HTTP status codes: `200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `404 Not Found`.
- Use `async`/`await` throughout all controller actions and data access methods.
- Document endpoints with XML comments so Swagger picks them up.

## Docker

- The API and PostgreSQL run as separate containers.
- Pass `CONNECTION_STRING` as an environment variable to the API container.
- Use a named Docker volume for PostgreSQL data persistence.
