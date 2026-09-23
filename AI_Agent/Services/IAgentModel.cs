using AI_Agent.Models;

namespace AI_Agent.Services;

public interface IAgentModel
{
    Task<AgentPlan> CreatePlanAsync(
        string userMessage,
        IReadOnlyList<string> tools,
        string? planningContext = null,
        CancellationToken cancellationToken = default);
    Task<string> FormatResponseAsync(string userMessage, object toolResult, CancellationToken cancellationToken = default);
}
