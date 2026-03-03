using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

builder.Configuration.AddEnvironmentVariables();

var db = builder.Configuration["CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(db))
{
    throw new InvalidOperationException("CONNECTION_STRING not set in environment or appsettings.json");
}

builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "To-do with extras API";
    config.Version = "v1";
});
builder.Services.AddDbContext<MyDbContext>(conf => { conf.UseNpgsql(db); });
var app = builder.Build();
await DatabaseSeeder.InitializeAsync(app.Services, builder.Configuration, db);

app.UseStaticFiles();

// Use CORS before controllers
app.UseCors();

app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();

app.Run();