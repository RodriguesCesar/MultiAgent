using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

const string AgentApiKeyHeaderName = "X-Agent-Api-Key";

var agentRateLimitPermit = ReadPositiveInt(builder.Configuration, "RateLimiting:Agent:PermitLimit");
var agentRateLimitWindowSeconds = ReadPositiveInt(builder.Configuration, "RateLimiting:Agent:WindowSeconds");
var agentRateLimitQueueLimit = ReadNonNegativeInt(builder.Configuration, "RateLimiting:Agent:QueueLimit");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Limite de requisições excedido",
            Detail = "Aguarde alguns instantes e tente novamente."
        };

        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
    };

    options.AddFixedWindowLimiter("agent-requests", limiterOptions =>
    {
        limiterOptions.PermitLimit = agentRateLimitPermit;
        limiterOptions.Window = TimeSpan.FromSeconds(agentRateLimitWindowSeconds);
        limiterOptions.QueueLimit = agentRateLimitQueueLimit;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddSingleton(sp =>
    KernelFactory.CreateKernel(sp.GetRequiredService<IConfiguration>()));

builder.Services.AddSingleton<PlannerAgent>();
builder.Services.AddSingleton<ResearchAgent>();
builder.Services.AddSingleton<CodeAgent>();
builder.Services.AddSingleton<ReviewAgent>();

builder.Services.AddSingleton<AgentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro inesperado",
            Detail = app.Environment.IsDevelopment() ? exception?.Message : "Consulte o traceId para suporte."
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.MapHealthChecks("/health");
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/agent", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var allowAnonymousInDevelopment = app.Configuration.GetValue("Authentication:AllowAnonymousInDevelopment", true);
    var configuredAgentApiKey = app.Configuration["Authentication:AgentApiKey"];
    var bypassAuthentication = app.Environment.IsDevelopment() && allowAnonymousInDevelopment;

    if (string.IsNullOrWhiteSpace(configuredAgentApiKey))
    {
        if (bypassAuthentication)
        {
            await next();
            return;
        }

        var configurationProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Configuração inválida",
            Detail = "Defina Authentication:AgentApiKey para proteger o endpoint /api/agent."
        };

        configurationProblem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(configurationProblem);
        return;
    }

    var providedApiKey = context.Request.Headers[AgentApiKeyHeaderName].ToString();
    if (!IsValidApiKey(configuredAgentApiKey, providedApiKey))
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Não autorizado",
            Detail = $"Informe o header {AgentApiKeyHeaderName} com uma API key válida."
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(problem);
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("agent-requests");
app.Run();

static int ReadPositiveInt(IConfiguration configuration, string key)
{
    var value = configuration.GetValue<int?>(key)
        ?? throw new InvalidOperationException($"Configuração obrigatória ausente: {key}");

    if (value <= 0)
        throw new InvalidOperationException($"Configuração inválida para {key}. Informe um número maior que zero.");

    return value;
}

static int ReadNonNegativeInt(IConfiguration configuration, string key)
{
    var value = configuration.GetValue<int?>(key)
        ?? throw new InvalidOperationException($"Configuração obrigatória ausente: {key}");

    if (value < 0)
        throw new InvalidOperationException($"Configuração inválida para {key}. Informe zero ou um número positivo.");

    return value;
}

static bool IsValidApiKey(string? expectedApiKey, string providedApiKey)
{
    if (string.IsNullOrWhiteSpace(expectedApiKey) || string.IsNullOrEmpty(providedApiKey))
        return false;

    var expected = Encoding.UTF8.GetBytes(expectedApiKey);
    var provided = Encoding.UTF8.GetBytes(providedApiKey);

    return expected.Length == provided.Length && CryptographicOperations.FixedTimeEquals(expected, provided);
}

public partial class Program;
