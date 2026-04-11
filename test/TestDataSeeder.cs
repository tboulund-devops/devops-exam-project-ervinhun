using server.DataAccess;

namespace test;

internal static class TestDataSeeder
{
    public static void SeedBaseData(MyDbContext db)
    {
        if (!db.Users.Any(u => u.Username == "system"))
        {
            db.Users.Add(new User
            {
                Username = "system",
                Email = "system@test.local"
            });
        }

        if (!db.Users.Any(u => u.Username == "test-user"))
        {
            db.Users.Add(new User
            {
                Username = "test-user",
                Email = "test-user@test.local"
            });
        }

        var backlogStatus = db.TodoTaskStatuses.FirstOrDefault(s => s.Name == "Backlog");
        if (backlogStatus == null)
        {
            backlogStatus = new TodoTaskStatus
            {
                Name = "Backlog",
                CreatedAt = DateTime.UtcNow
            };
            db.TodoTaskStatuses.Add(backlogStatus);
        }

        foreach (var statusName in new[] { "To-do", "Doing", "Review", "Done" })
        {
            if (!db.TodoTaskStatuses.Any(s => s.Name == statusName))
            {
                db.TodoTaskStatuses.Add(new TodoTaskStatus
                {
                    Name = statusName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        db.SaveChanges();

        var testUserId = db.Users
            .Where(u => u.Username == "test-user")
            .Select(u => u.Id)
            .First();

        if (!db.TaskItems.Any(t => t.Title == "Seeded default task" && t.DeletedAt == null))
        {
            db.TaskItems.Add(new TaskItem
            {
                Title = "Seeded default task",
                Description = "Default task for integration tests",
                StatusId = backlogStatus.Id,
                AssigneeId = testUserId
            });
            db.SaveChanges();
        }

        if (!db.Notifications.Any(n => n.UserId == testUserId && n.Message == "Seeded unread notification"))
        {
            db.Notifications.Add(new Notification
            {
                UserId = testUserId,
                Message = "Seeded unread notification",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            });
        }

        if (!db.Notifications.Any(n => n.UserId == testUserId && n.Message == "Seeded read notification"))
        {
            db.Notifications.Add(new Notification
            {
                UserId = testUserId,
                Message = "Seeded read notification",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
        }

        db.SaveChanges();
    }
}

