using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MultiAgent.API.Services;

namespace MultiAgent.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentController(AgentService agentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(MultiAgent.API.Models.AgentContextModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] AgentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var response = await agentService.Execute(request.Message, cancellationToken);
        return Ok(response);
    }
}

public record AgentRequest(
    [Required(AllowEmptyStrings = false)]
    [MinLength(5)]
    [MaxLength(4000)]
    string Message
    );