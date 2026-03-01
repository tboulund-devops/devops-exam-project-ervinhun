using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;
using Testcontainers.PostgreSql;

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

            var ctx = services.BuildServiceProvider().GetRequiredService<MyDbContext>();
            ctx.Database.EnsureCreated();
        });

        builder.ConfigureServices(services =>
        {
            // Build the service provider and create the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<MyDbContext>();

            // Ensure database is created
            db.Database.EnsureCreated();
        });
    }
}