using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.DataAccess;
using Task = System.Threading.Tasks.Task;

namespace server.Utils;

public class DatabaseSeeder
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
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "DataAccess", "schema.sql");
            var sql = await File.ReadAllTextAsync(schemaPath);

            await using var createCmd = new NpgsqlCommand(sql, connection);
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedDataAsync(MyDbContext context)
    {
        // Seed Task Statuses
        if (!await context.TodoTaskStatuses.AnyAsync())
        {
            context.TodoTaskStatuses.AddRange(
                new TodoTaskStatus { Name = "To-do" },
                new TodoTaskStatus { Name = "Doing" },
                new TodoTaskStatus { Name = "Done" }
            );

            await context.SaveChangesAsync();
        }

        // Seed Random User
        if (!await context.Users.AnyAsync())
        {
            var random = Guid.NewGuid().ToString().Substring(0, 8);

            var user = new User
            {
                Username = $"user_{random}",
                Email = $"user_{random}@example.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }
}