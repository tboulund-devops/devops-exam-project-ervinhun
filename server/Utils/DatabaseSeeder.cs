using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.DataAccess;
using Task = System.Threading.Tasks.Task;

namespace server.Utils;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(IServiceProvider services,
        IConfiguration configuration, string connectionString)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        await EnsureDatabaseSchemaAsync(connectionString);
        await SeedDataAsync(context);
    }

    private static async Task EnsureDatabaseSchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Check if one known table exists
        const string checkTableSql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'task_item'
            );";

        await using var checkCmd = new NpgsqlCommand(checkTableSql, connection);
        var exists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

        if (!exists)
        {
            try
            {
                var schemaPath = Path.Combine(AppContext.BaseDirectory, "DataAccess", "schema.sql");
                var sql = await File.ReadAllTextAsync(schemaPath);

                await using var createCmd = new NpgsqlCommand(sql, connection);
                await createCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to initialize database schema: {ex}");
                throw;
            }
        }
    }

    private static async Task SeedDataAsync(MyDbContext context)
    {
        // Seed Task Statuses
        if (!await context.TodoTaskStatuses.AnyAsync())
        {
            context.TodoTaskStatuses.AddRange(
                new TodoTaskStatus { Name = "Backlog" },
                new TodoTaskStatus { Name = "To-do" },
                new TodoTaskStatus { Name = "Doing" },
                new TodoTaskStatus { Name = "Review" },
                new TodoTaskStatus { Name = "Done" }
            );

            await context.SaveChangesAsync();
        }

        // Seed Random User
        if (!await context.Users.AnyAsync(u => u.Username == "system"))
        {
            var random = Guid.NewGuid().ToString("N")[..8];
            context.Users.AddRange(
                new User { Username = "system", Email = "no-reply@system.com" },
                new User { Username = $"user_{random}", Email = $"user_{random}@example.com" }
            );
            await context.SaveChangesAsync();
        }
    }
}