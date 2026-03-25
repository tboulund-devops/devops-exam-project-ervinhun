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

> **Note:** `ASPNETCORE_ENVIRONMENT` controls the standard ASP.NET Core hosting environment (for example, `Development`, `Production`, or `Test`). The current `server/Program.cs` does not branch on this value; integration tests customize services and database setup via `CustomWebApplicationFactory` in the `test` project.

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
- When controllers and service classes are present, keep controllers thin by delegating business logic to services or shared utilities (for example, helpers in `Utils/`).
- When a dedicated **database schema** file is introduced (for example, under a `server/DataAccess/` folder and copied to the output directory), ensure any seeding component (such as a `DatabaseSeeder`) applies it at startup in non-Test environments.
- When using database triggers to maintain timestamps (for example, a `set_updated_at()` trigger defined in the schema), rely on them to keep `updated_at` current on `UPDATE` for relevant tables instead of updating `updated_at` manually in application code.
- When task history tracking is implemented (for example, via a `SaveTaskToHistory` helper or similar mechanism), ensure it records changes to key fields such as `Title`, `Description`, and `AssigneeId`.
- When authentication is not yet implemented, use a consistent placeholder user (for example, a `system` user with username `"system"`) for history records.

## API Conventions

- Use RESTful resource naming: `GET /tasks`, `POST /tasks`, `GET /tasks/{id}`, `PUT /tasks/{id}`, `DELETE /tasks/{id}`.
- Return appropriate HTTP status codes: `200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `404 Not Found`.
- Use `async`/`await` throughout all controller actions and data access methods.
- Document endpoints (for example, with XML comments) so they can be picked up by Swagger / OpenAPI tooling when it is enabled.

## Docker

- The API and PostgreSQL run as separate containers.
- Pass `CONNECTION_STRING` as an environment variable to the API container.
- Use a named Docker volume for PostgreSQL data persistence.

## Critical Requirements (MANDATORY)

- Every code change MUST include unit or integration tests.
- Minimum 80% test coverage for all new or modified code.
- Code without tests is considered incomplete.
- Do not implement features without verifying them through tests.

## Testing Guidelines

- Prefer integration tests using Testcontainers for API behavior.
- Use unit tests for business logic in isolation.
- Tests must cover:
  - Success cases
  - Failure cases
  - Edge cases
- When fixing bugs:
  1. First write a failing test.
  2. Then implement the fix.
  3. Ensure the test passes.
- Do not modify or remove existing tests unless they are incorrect.

## Handling Code Review Comments

When addressing review comments:

- Fix the root cause, not just the visible issue.
- Update or add tests to reflect the fix.
- Keep changes minimal and focused.
- Do not introduce unrelated refactoring.
- Ensure all tests pass before finishing.

Avoid:
- Superficial fixes
- Ignoring failing tests
- Changing behavior without test coverage

## Definition of Done

A task or feature is complete only if:

- Code is implemented.
- Tests are added (≥80% coverage for new code).
- All tests pass.
- API behavior is validated via integration tests.
- Code follows repository conventions.
