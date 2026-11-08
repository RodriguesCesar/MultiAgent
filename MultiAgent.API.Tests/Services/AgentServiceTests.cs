using Microsoft.Extensions.Configuration;
using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;
using Xunit;

namespace MultiAgent.API.Tests.Services;

public class AgentServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Execute_WhenMessageIsInvalid_ThrowsArgumentException(string message)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.Execute(message));
    }

    private static AgentService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "gpt-4o",
                ["OpenAI:ApiKey"] = "test-key"
            })
            .Build();

        var kernel = KernelFactory.CreateKernel(configuration);
        return new AgentService(
            new PlannerAgent(kernel),
            new ResearchAgent(kernel),
            new CodeAgent(kernel),
            new ReviewAgent(kernel));
    }
}
