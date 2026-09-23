using System.Text.Json;

namespace AI_Agent.Models;

public sealed class AgentPlan
{
    public string Tool { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Arguments { get; set; } = [];
    public string? Message { get; set; }
}
