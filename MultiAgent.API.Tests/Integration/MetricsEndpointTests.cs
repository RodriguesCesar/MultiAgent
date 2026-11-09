using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MultiAgent.API.Tests.Integration;

public class MetricsEndpointTests
{
    [Fact]
    public async Task MetricsEndpoint_ShouldExposeSecurityCounters()
    {
        await using var factory = new MetricsApiFactory();
        using var client = factory.CreateClient();

        var authFailureResponse = await client.PostAsJsonAsync("/api/agent", new { message = "1234" });
        Assert.Equal(HttpStatusCode.Unauthorized, authFailureResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/metrics");
        var metricsBody = await metricsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        Assert.Contains("agent_auth_failures_total", metricsBody);
    }

    private sealed class MetricsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenAI:Model"] = "gpt-4o",
                    ["OpenAI:ApiKey"] = "test-key",
                    ["Authentication:AgentApiKeys:0"] = "metrics-test-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }
}
