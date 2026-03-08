using Microsoft.SemanticKernel;

namespace MultiAgent.API.Agents
{
    public class ResearchAgent(Kernel kernel)
    {
        public async Task<string> Research(Models.AgentContextModel context)
        {
            var prompt = """
                    Você é um agente de pesquisa.
                    Com base no plano criado pelo agente de planejamento, faça uma pesquisa para encontrar as informações necessárias para que o agente programador possa escrever o código.
                    Plano criado pelo agente de planejamento:
                    {{$plan}}
                    Responda de forma curta e objetiva.
                    Use no máximo 5 linhas.
                    """;

            var function = kernel.CreateFunctionFromPrompt(prompt);

            var result = await kernel.InvokeAsync(
                function,
                new()
                {
                    ["plan"] = context.Plan
                });

            return result.ToString();
        }
    }
}
