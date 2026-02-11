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
**Feature 1:** Create a new task 

**Feature 2:** View tasks (list all tasks + get single task by id).

### Week 7
*Winter vacation - nothing planned.*

### Week 8
**Feature 1:** Move task between workflow states (To-Do, Doing, Review, Done)

**Feature 2:** Task status history (store when status changes + from/to)

### Week 9
**Feature 1:** Edit task

**Feature 2:** Remove task

### Week 10
**Feature 1:** Filter tasks (e.g. only “Doing”) via query parameters.

**Feature 2:** Sort tasks by creation date or last update via query parameters.

### Week 11
**Feature 1:** Assign task to a user (assignee = name or id).

**Feature 2:** Task comments (add and view comments on a task).

### Week 12
**Feature 1:** Notifications generated when a task is moved to Done.

**Feature 2:** Notification overview (list notifications + mark as read).

### Week 13
**Feature 1:** Archive (soft delete) tasks 

**Feature 2:** Reopen completed tasks (Done → Doing/To-Do) with rules

### Week 14
*Easter vacation - nothing planned.*

### Week 15
**Feature 1:** Tasks can have a due date.

**Feature 2:** Overdue tasks (return tasks that are overdue).

### Week 16
**Feature 1:** Search tasks (by title/description)

**Feature 2:** update status/assignee for multiple tasks in one request

### Week 17
**Feature 1:** System works without bugs (stability + bug fixing).

**Feature 2:** System is stable and ready for final demonstration.
