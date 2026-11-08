using Microsoft.Extensions.Configuration;
using MultiAgent.API.Factories;
using Xunit;

namespace MultiAgent.API.Tests.Factories;

public class KernelFactoryTests
{
    [Fact]
    public void CreateKernel_WhenApiKeyIsMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "gpt-4o"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => KernelFactory.CreateKernel(configuration));

        Assert.Contains("OpenAI:ApiKey", exception.Message);
    }
}
