using Microsoft.SemanticKernel;
using MultiAgent.API.Models;

namespace MultiAgent.API.Agents;

public class CodeAgent(Kernel kernel)
{
    public async Task<string> GenerateCode(AgentContextModel context)
    {
        var prompt = """
                Plano:
                {{$plan}}
                Pesquisa:
                {{$research}}
                Gere código em C# seguindo esse plano.
                """;

        var function = kernel.CreateFunctionFromPrompt(prompt);

        var result = await kernel.InvokeAsync(
            function,
            new()
            {
                ["plan"] = context.Plan,
                ["research"] = context.Research
            });

        return result.ToString();
    }
}
