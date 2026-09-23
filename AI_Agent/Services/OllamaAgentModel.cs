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
                    content = "You are a banking request planner. Select exactly one available MCP tool. Return only JSON with fields: tool, arguments, message. Use an empty arguments object when no tool applies. Never invent account IDs, names, or amounts."
                },
                new
                {
                    role = "system",
                    content = $"Available MCP tools and exact arguments: {string.Join("; ", tools.Select(GetToolContract))}"
                },
                new
                {
                    role = "user",
                    content = userMessage
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
