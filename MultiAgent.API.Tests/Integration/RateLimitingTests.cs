using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MultiAgent.API.Tests.Integration;

public class RateLimitingTests
{
    [Fact]
    public async Task AgentEndpoint_ShouldReturnTooManyRequests_WhenLimitIsExceeded()
    {
        await using var factory = new RateLimitTestApiFactory();
        using var client = factory.CreateClient();

        var payload = new { message = "1234" };
        const int permitLimit = 20;

        for (var i = 0; i < permitLimit; i++)
        {
            var response = await client.PostAsJsonAsync("/api/agent", payload);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/agent", payload);

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }

    private sealed class RateLimitTestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenAI:Model"] = "gpt-4o",
                    ["OpenAI:ApiKey"] = "test-key",
                    ["Authentication:AllowAnonymousInDevelopment"] = "true"
                });
            });
        }
    }
}
