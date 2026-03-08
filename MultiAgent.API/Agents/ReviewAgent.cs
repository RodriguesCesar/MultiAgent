using Microsoft.SemanticKernel;
using MultiAgent.API.Models;

namespace MultiAgent.API.Agents;

public class ReviewAgent(Kernel kernel)
{
    public async Task<string> Review(AgentContextModel context)
    {
        var prompt = """
            Plano:
            {{$plan}}
            Código gerado:
            {{$code}}
            Revise e sugira melhorias.
            Responda de forma curta e objetiva.
            Use no máximo 10 linhas.
            """;

        var function = kernel.CreateFunctionFromPrompt(prompt);

        var result = await kernel.InvokeAsync(
            function,
            new()
            {
                ["plan"] = context.Plan,
                ["code"] = context.Code
            });

        return result.ToString();
    }
}
