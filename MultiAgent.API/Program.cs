using MultiAgent.API.Agents;
using MultiAgent.API.Factories;
using MultiAgent.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();
builder.Services.AddOpenApi();

var kernel = KernelFactory.CreateKernel();
builder.Services.AddSingleton(kernel);

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

app.UseAuthorization();
app.MapControllers();
app.Run();
