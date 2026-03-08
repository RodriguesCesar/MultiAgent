using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiAgent.API.Services;

namespace MultiAgent.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentController(AgentService agentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AgentRequest request)
    {
        var response = await agentService.Execute(request.message);
        return Ok(response);
    }
}

/* Modelo de dados para a requisição */
public record AgentRequest(
        string message
    );