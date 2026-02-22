using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.Generation;

namespace server;

public static class GenerateApiClientsExtensions
{
    public static async Task<string> GenerateApiClientsFromOpenApi(
        this WebApplication app,
        string clientOutputPath,
        string openApiOutputPath)
    {
        var document = await app.Services.GetRequiredService<IOpenApiDocumentGenerator>()
            .GenerateAsync("v1");

        var openApiJson = document.ToJson();

        Directory.CreateDirectory(Path.GetDirectoryName(openApiOutputPath)!);
        await File.WriteAllTextAsync(openApiOutputPath, openApiJson);

        var documentFromJson = await OpenApiDocument.FromJsonAsync(openApiJson);

        var settings = new TypeScriptClientGeneratorSettings
        {
            Template = TypeScriptTemplate.Fetch,
            TypeScriptGeneratorSettings =
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String,
                NullValue = TypeScriptNullValue.Undefined,
                TypeScriptVersion = 5.2m,
                GenerateCloneMethod = false,
                MarkOptionalProperties = true,
                GenerateConstructorInterface = true,
                ConvertConstructorInterfaceData = true
            }
        };

        var generator = new TypeScriptClientGenerator(documentFromJson, settings);
        var code = generator.GenerateFile();

        Directory.CreateDirectory(Path.GetDirectoryName(clientOutputPath)!);
        await File.WriteAllTextAsync(clientOutputPath, code);

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("OpenAPI JSON with documentation saved at: " + openApiOutputPath);
        logger.LogInformation("TypeScript client generated at: " + clientOutputPath);



        return openApiOutputPath;
    }
}