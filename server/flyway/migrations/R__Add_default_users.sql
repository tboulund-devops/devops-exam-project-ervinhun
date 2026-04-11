-- Seed the same default users as DatabaseSeeder.SeedDataAsync.
-- This repeatable migration is idempotent and safe to run multiple times.

INSERT INTO users (username, email)
SELECT 'system', 'no-reply@system.com'
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE username = 'system'
);

-- DatabaseSeeder creates one random user_* only when system is first seeded.
-- Here we add one generated user_* only if no such user exists yet.
WITH generated_user AS (
    SELECT 'user_' || SUBSTRING(gen_random_uuid()::text, 1, 8) AS username
)
INSERT INTO users (username, email)
SELECT gu.username, gu.username || '@example.com'
FROM generated_user gu
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE username LIKE 'user\_%' ESCAPE '\'
);

