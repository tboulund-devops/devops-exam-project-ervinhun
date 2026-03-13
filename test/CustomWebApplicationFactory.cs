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
        // Prefer applying the same schema.sql used in production so that tests
        // see the same triggers, functions, indexes, etc. If schema.sql is not
        // available, fall back to EnsureCreated() to preserve existing behavior.
        var baseDirectory = AppContext.BaseDirectory;
        var schemaPath = Path.Combine(baseDirectory, "schema.sql");

        if (File.Exists(schemaPath))
        {
            var sql = File.ReadAllText(schemaPath);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                db.Database.ExecuteSqlRaw(sql);
            }
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }
}