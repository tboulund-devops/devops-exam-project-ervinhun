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

var db = builder.Configuration.GetSection("CONNECTION_STRING").Value;
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

app.UseStaticFiles();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();

// app.GenerateApiClientsFromOpenApi("../client/src/generated-ts-client.ts", "./openapi.json").GetAwaiter().GetResult();

await DatabaseSeeder.InitializeAsync(app.Services, builder.Configuration, db);

app.Run();