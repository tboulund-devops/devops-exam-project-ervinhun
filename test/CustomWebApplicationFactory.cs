using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;
using Testcontainers.PostgreSql;
using System.IO;

namespace test;

public class CustomWebApplicationFactory : WebApplicationFactory<server.Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        Environment.SetEnvironmentVariable("FEATURE_HUB_BYPASS", "true");
        Environment.SetEnvironmentVariable("FEATURE_HUB_URL", "http://localhost");
        Environment.SetEnvironmentVariable("FEATURE_HUB_API_KEY", "test-api-key");

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<MyDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType == typeof(MyDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add Testcontainers PostgreSQL database
            if (_container != null)
            {
                var connectionString = _container.GetConnectionString();
                services.AddDbContext<MyDbContext>(options => { options.UseNpgsql(connectionString); });
            }

            // Build a scoped service provider and ensure the database is created once
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<MyDbContext>();
            ApplyDatabaseSchema(db);

            TestDataSeeder.SeedBaseData(db);
        });
    }

    private static void ApplyDatabaseSchema(MyDbContext db)
    {
        // Apply the Flyway migration SQL files so tests use the same schema path
        // as deployment.
        var baseDirectory = AppContext.BaseDirectory;
        var migrationsPath = Path.Combine(baseDirectory, "flyway", "migrations");

        if (Directory.Exists(migrationsPath))
        {
            var scripts = Directory.GetFiles(migrationsPath, "*.sql", SearchOption.TopDirectoryOnly)
                .OrderBy(static file =>
                {
                    var name = Path.GetFileName(file);
                    if (name.StartsWith("V", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (name.StartsWith("R", StringComparison.OrdinalIgnoreCase)) return 1;
                    return 2;
                })
                .ThenBy(static file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var scriptPath in scripts)
            {
                var sql = File.ReadAllText(scriptPath);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    db.Database.ExecuteSqlRaw(sql);
                }
            }

            if (scripts.Count > 0)
            {
                return;
            }
        }

        db.Database.EnsureCreated();
    }
}