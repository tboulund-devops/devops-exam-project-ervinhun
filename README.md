# To-do with extras

This project is a simple ASP.NET Core Web API that manages tasks using a relational database.
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
**Feature 1:** Database planning, and connectiong database to the application

**Feature 2:** Creating Dockerfile and docker-compose

### Week 7
*Winter vacation - nothing planned.*

### Week 8
**Feature 1:** Startup for Unit test

**Feature 2:** Writing unit tests for this endpoints

### Week 9
**Feature 1:** Creating entities and endpoints for CRUD

**Feature 2:** First version deployment

### Week 10
**Feature 1:** Fix env variables for the deployed project, ensure that the database is connected

**Feature 2:** Set up Github Actions automatic deployment

### Week 11
**Feature 1:** Set up E2E testing

**Feature 2:** Performance testing

### Week 12
**Feature 1:** Track deployment frequency — document and measure how often we deploy to production

**Feature 2:** Track lead time for changes — measure time from commit to production deployment using GitHub Actions timestamps

### Week 13
**Feature 1:** Creating structured logging for the errors

**Feature 2:** Send the log files back to the developers, so they can fix the errors

### Week 14
*Easter vacation - nothing planned.*

### Week 15
**Feature 1:** Health check endpoints — add /health and /ready endpoints

**Feature 2:** Rolling deployment - setup toggle features

### Week 16
**Feature 1:** To be decided

**Feature 2:** To be decided

### Week 17
**Feature 1:** Fixing upcomming errors and bugs

**Feature 2:** Finalising the project
