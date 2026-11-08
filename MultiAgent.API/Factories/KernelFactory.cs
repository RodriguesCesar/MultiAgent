using Microsoft.SemanticKernel;

namespace MultiAgent.API.Factories;

public class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();
        var modelId = configuration["OpenAI:Model"] ?? "gpt-4o";
        var apiKey = configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey não configurada. Defina por variável de ambiente ou appsettings.Local.json.");

        builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey
            );

        return builder.Build();
    }
}
