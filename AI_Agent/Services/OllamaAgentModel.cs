using AI_Agent.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AI_Agent.Services;

public sealed class OllamaAgentModel : IAgentModel
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaAgentModel(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string userMessage,
        IReadOnlyList<string> tools,
        string? planningContext = null,
        CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:Model"] ?? "llama3.2";
        var request = new
        {
            model,
            stream = false,
            format = "json",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a banking agent planner. Select the next available MCP tool needed to answer the user's request. You may request tools across multiple steps. Use results from the planning context for later arguments. If the request can be answered from the context, return an empty tool and put the final answer in message. Return only JSON with fields: tool, arguments, message. Use an empty arguments object when no tool applies. Never invent account IDs, names, or amounts. Do not repeat a completed tool call unless necessary."
                },
                new
                {
                    role = "system",
                    content = $"Available MCP tools and exact arguments: {string.Join("; ", tools.Select(GetToolContract))}"
                },
                new
                {
                    role = "user",
                    content = $"User request:\n{userMessage}\n\nPlanning context from completed tool calls:\n{planningContext ?? "(none)"}"
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        if (string.IsNullOrWhiteSpace(payload.Message?.Content))
        {
            throw new InvalidOperationException("Ollama returned no planning content.");
        }

        return JsonSerializer.Deserialize<AgentPlan>(payload.Message.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Ollama returned an invalid agent plan.");
    }

    public async Task<string> FormatResponseAsync(
        string userMessage,
        object toolResult,
        CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:Model"] ?? "llama3.2";
        var request = new
        {
            model,
            stream = false,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a banking assistant formatting tool results. Answer the user's request using only the supplied tool result. If the user requests a table, return a plain-text Markdown table. Be concise. Do not invent or omit data. Return only the final response text, without JSON wrappers."
                },
                new
                {
                    role = "user",
                    content = $"User request:\n{userMessage}\n\nMCP tool result JSON:\n{JsonSerializer.Serialize(toolResult)}"
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return string.IsNullOrWhiteSpace(payload.Message?.Content)
            ? throw new InvalidOperationException("Ollama returned no formatted response.")
            : payload.Message.Content.Trim();
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private static string GetToolContract(string tool)
    {
        return tool switch
        {
            "list_accounts" => "list_accounts() - list all accounts and their current balances",
            "create_account" => "create_account(customerName: string, initialDeposit: number)",
            "get_account_balance" => "get_account_balance(accountId: string)",
            "deposit" => "deposit(accountId: string, amount: number)",
            "withdraw" => "withdraw(accountId: string, amount: number)",
            "get_transactions" => "get_transactions(accountId: string, count: number)",
            "get_customer_profile" => "get_customer_profile(customerId: string)",
            "get_account_status" => "get_account_status(accountId: string)",
            _ => $"{tool}()"
        };
    }
}
