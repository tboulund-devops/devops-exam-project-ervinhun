# To-do with extras

Description: This project is a simple ASP.NET Core Web API that manages tasks using a relational database.
Each task moves through multiple workflow states — To-Do, Doing, Review, and Done — allowing basic task tracking similar to a lightweight Kanban board.

The application exposes REST endpoints for CRUD tasks, as well as to change their current status. Data is persisted in a relational database.

## Tech-stack

* ASP.NET Core Web API (.NET 10)
* Entity Framework Core
* Swagger API
* PostgreSQL
* Docker + Docker Compose

## Architecture

The application is built as a stateless ASP.NET Core Web API using Entity Framework Core for data access. Task data is stored in a relational database, where each task has a workflow status (To-Do, Doing, Review, Done).
The API and database run in separate Docker containers, with configuration provided through environment variables. This simple architecture supports CI/CD pipelines, automated testing, database migrations, and easy deployment across different environments.

## Feature plan

> [!NOTE]
> For each week below write a short description of the features you plan to build for your project this week.

[...]

### Week 5
*Kick-off week - no features to be planned here*

### Week 6
**Feature 1:** [...]

**Feature 2:** [...]

### Week 7
*Winter vacation - nothing planned.*

### Week 8
**Feature 1:** [...]

**Feature 2:** [...]

### Week 9
**Feature 1:** [...]

**Feature 2:** [...]

### Week 10
**Feature 1:** [...]

**Feature 2:** [...]

### Week 11
**Feature 1:** [...]

**Feature 2:** [...]

### Week 12
**Feature 1:** [...]

**Feature 2:** [...]

### Week 13
**Feature 1:** [...]

**Feature 2:** [...]

### Week 14
*Easter vacation - nothing planned.*

### Week 15
**Feature 1:** [...]

**Feature 2:** [...]

### Week 16
**Feature 1:** [...]

**Feature 2:** [...]

### Week 17
**Feature 1:** [...]

**Feature 2:** [...]
