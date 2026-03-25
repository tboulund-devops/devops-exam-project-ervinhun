-- Seed the same default task types as DatabaseSeeder.SeedDataAsync.
-- This repeatable migration is idempotent and safe to run multiple times.

INSERT INTO todo_task_status (name)
VALUES
    ('Backlog'),
    ('To-do'),
    ('Doing'),
    ('Review'),
    ('Done')
ON CONFLICT (name) DO NOTHING;

