using AI_Agent.Models;

namespace AI_Agent.Services;

public interface IAgentModel
{
    Task<AgentPlan> CreatePlanAsync(string userMessage, IReadOnlyList<string> tools, CancellationToken cancellationToken = default);
}
