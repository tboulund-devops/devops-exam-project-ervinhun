using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

builder.Configuration.AddEnvironmentVariables();

var db = builder.Configuration["CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(db) && !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException("CONNECTION_STRING not set in environment or appsettings.json");
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
    await DatabaseSeeder.InitializeAsync(app.Services, builder.Configuration, db);
}

app.UseStaticFiles();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();


await app.RunAsync();