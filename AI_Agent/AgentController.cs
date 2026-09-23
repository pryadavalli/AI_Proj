using AI_Agent.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Agent.Controllers;

[ApiController]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly Services.AgentService _agent;

    public AgentController(Services.AgentService agent)
    {
        _agent = agent;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        var result = await _agent.AskAsync(request.Message);
        return Ok(result);
    }
}