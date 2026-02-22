using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace test;

public class CustomWebApplicationFactory : WebApplicationFactory<server.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Later;
            // - swap DB to Testcontainers / InMemory
            // ü replace external service
            // - add fake auth
        });
    }
}