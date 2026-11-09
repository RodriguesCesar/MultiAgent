using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
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
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(SecurityTelemetry.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        SecurityTelemetry.RateLimitRejections.Add(1);
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MultiAgent.API.Security");
        logger.LogWarning(
            "Rate limit exceeded for path {Path}. TraceId={TraceId} RemoteIp={RemoteIp}",
            context.HttpContext.Request.Path,
            context.HttpContext.TraceIdentifier,
            context.HttpContext.Connection.RemoteIpAddress?.ToString());

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
app.MapPrometheusScrapingEndpoint("/metrics");
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/agent", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var allowAnonymousInDevelopment = app.Configuration.GetValue("Authentication:AllowAnonymousInDevelopment", true);
    var configuredAgentApiKeys = ReadConfiguredAgentApiKeys(app.Configuration);
    var bypassAuthentication = app.Environment.IsDevelopment() && allowAnonymousInDevelopment;
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("MultiAgent.API.Security");

    if (configuredAgentApiKeys.Count == 0)
    {
        if (bypassAuthentication)
        {
            logger.LogWarning(
                "Authentication bypass enabled in Development due to AllowAnonymousInDevelopment and no configured API key. TraceId={TraceId}",
                context.TraceIdentifier);
            await next();
            return;
        }

        SecurityTelemetry.AuthConfigErrors.Add(1);
        logger.LogError(
            "Agent authentication is misconfigured: no API keys configured. Path={Path} TraceId={TraceId}",
            context.Request.Path,
            context.TraceIdentifier);

        var configurationProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Configuração inválida",
            Detail = "Defina Authentication:AgentApiKeys (ou Authentication:AgentApiKey) para proteger o endpoint /api/agent."
        };

        configurationProblem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(configurationProblem);
        return;
    }

    var providedApiKey = context.Request.Headers[AgentApiKeyHeaderName].ToString();
    if (!IsValidApiKey(configuredAgentApiKeys, providedApiKey))
    {
        SecurityTelemetry.AuthFailures.Add(1);
        logger.LogWarning(
            "Invalid API key for agent endpoint. Path={Path} TraceId={TraceId} RemoteIp={RemoteIp}",
            context.Request.Path,
            context.TraceIdentifier,
            context.Connection.RemoteIpAddress?.ToString());

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

    SecurityTelemetry.AuthSuccess.Add(1);
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

static IReadOnlyList<string> ReadConfiguredAgentApiKeys(IConfiguration configuration)
{
    var configuredKeys = configuration.GetSection("Authentication:AgentApiKeys").Get<string[]>() ?? [];
    var keys = new List<string>();

    foreach (var key in configuredKeys)
    {
        if (!string.IsNullOrWhiteSpace(key))
            keys.Add(key.Trim());
    }

    var fallbackSingleKey = configuration["Authentication:AgentApiKey"];
    if (!string.IsNullOrWhiteSpace(fallbackSingleKey))
        keys.Add(fallbackSingleKey.Trim());

    return keys.Distinct(StringComparer.Ordinal).ToArray();
}

static bool IsValidApiKey(IReadOnlyList<string> expectedApiKeys, string providedApiKey)
{
    if (expectedApiKeys.Count == 0 || string.IsNullOrEmpty(providedApiKey))
        return false;

    var provided = Encoding.UTF8.GetBytes(providedApiKey);
    foreach (var expectedApiKey in expectedApiKeys)
    {
        var expected = Encoding.UTF8.GetBytes(expectedApiKey);
        if (expected.Length == provided.Length && CryptographicOperations.FixedTimeEquals(expected, provided))
            return true;
    }

    return false;
}

public partial class Program;

static class SecurityTelemetry
{
    public const string MeterName = "MultiAgent.API.Security";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> AuthFailures = Meter.CreateCounter<long>(
        "agent_auth_failures_total",
        unit: "{request}",
        description: "Total de tentativas de autenticação inválidas no endpoint de agente.");

    public static readonly Counter<long> AuthSuccess = Meter.CreateCounter<long>(
        "agent_auth_success_total",
        unit: "{request}",
        description: "Total de autenticações válidas no endpoint de agente.");

    public static readonly Counter<long> AuthConfigErrors = Meter.CreateCounter<long>(
        "agent_auth_config_errors_total",
        unit: "{request}",
        description: "Total de requisições bloqueadas por configuração inválida de autenticação.");

    public static readonly Counter<long> RateLimitRejections = Meter.CreateCounter<long>(
        "agent_rate_limit_rejections_total",
        unit: "{request}",
        description: "Total de requisições rejeitadas por rate limiting no endpoint de agente.");
}
