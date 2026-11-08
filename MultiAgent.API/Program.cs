using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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
app.UseAuthorization();
app.MapControllers();
app.Run();
