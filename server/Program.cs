using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Add CORS — permissive in Development; in other environments origins are
// restricted to the comma-separated list in the ALLOWED_ORIGINS env variable.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"];
            if (!string.IsNullOrWhiteSpace(allowedOrigins))
            {
                policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                throw new InvalidOperationException("In non-development environments, ALLOWED_ORIGINS must be set to a non-empty comma-separated list of origins for CORS.");
            }
        }
    });
});

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

builder.Configuration.AddEnvironmentVariables();

var db = builder.Configuration["CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(db) && !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException("CONNECTION_STRING not set in environment");
}

builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "To-do with extras API";
    config.Version = "v1";
});

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<MyDbContext>(conf => { conf.UseNpgsql(db); });
}

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Test"))
{
    await DatabaseSeeder.InitializeAsync(app.Services, db!);
}

app.UseStaticFiles();

// Use CORS before controllers
app.UseCors();

app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();

app.Run();