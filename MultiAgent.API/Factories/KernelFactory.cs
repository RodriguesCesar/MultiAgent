using Microsoft.SemanticKernel;

namespace MultiAgent.API.Factories;

public class KernelFactory
{

    public static Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
                modelId: "gpt-4o",
                apiKey: "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"
            );

        return builder.Build();

    }
}
