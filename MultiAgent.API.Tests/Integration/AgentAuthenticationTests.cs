using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MultiAgent.API.Tests.Integration;

public class AgentAuthenticationTests
{
    [Fact]
    public async Task AgentEndpoint_ShouldReturnUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        await using var factory = new AuthenticationTestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/agent", new { message = "1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AgentEndpoint_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        await using var factory = new AuthenticationTestApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Agent-Api-Key", "invalid-key");

        var response = await client.PostAsJsonAsync("/api/agent", new { message = "1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AgentEndpoint_ShouldAllowRequest_WhenApiKeyIsValid()
    {
        await using var factory = new AuthenticationTestApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Agent-Api-Key", "active-api-key");

        var response = await client.PostAsJsonAsync("/api/agent", new { message = "1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AgentEndpoint_ShouldAllowRequest_WhenTransitionApiKeyIsUsed()
    {
        await using var factory = new AuthenticationTestApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Agent-Api-Key", "transition-api-key");

        var response = await client.PostAsJsonAsync("/api/agent", new { message = "1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class AuthenticationTestApiFactory : WebApplicationFactory<Program>
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
                    ["Authentication:AgentApiKeys:0"] = "active-api-key",
                    ["Authentication:AgentApiKeys:1"] = "transition-api-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "false"
                });
            });
        }
    }
}
