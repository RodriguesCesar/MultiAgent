using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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

public partial class Program;
