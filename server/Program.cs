using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Add CORS — permissive in Development only; in production the API is expected
// to be served from the same origin, so no cross-origin policy is defined.
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
    await DatabaseSeeder.InitializeAsync(app.Services, db);
}

app.UseStaticFiles();

// Use CORS before controllers
app.UseCors();

app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();

app.Run();