using Microsoft.SemanticKernel;
using MultiAgent.API.Models;

namespace MultiAgent.API.Agents
{
    public class PlannerAgent(Kernel kernel)
    {
        public async Task<string> CreatePlan(AgentContextModel context, CancellationToken cancellationToken = default)
        {
            var prompt = """
                Você é um arquiteto de software.
                Pedido do usuário:
                {{$request}}
                Crie um plano técnico objetivo para que outros agentes possam executar.
                Responda de forma curta e objetiva.
                Use no máximo 5 linhas.
                """;

            var function = kernel.CreateFunctionFromPrompt(prompt);

            var result = await kernel.InvokeAsync(
                function,
                new()
                {
                    ["request"] = context.UserRequest
                },
                cancellationToken: cancellationToken);

            return result.ToString();
        }
    }
}
