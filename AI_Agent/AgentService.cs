using AI_Agent.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AI_Agent.Services;

public sealed class AgentService
{
    private readonly McpClientService _mcp;
    private readonly IAgentModel _model;

    public AgentService(McpClientService mcp, IAgentModel model)
    {
        _mcp = mcp;
        _model = model;
    }

    public async Task<AgentResponse> AskAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new AgentResponse
            {
                Answer = new { message = "Please send a valid banking request." }
            };
        }

        var tools = await _mcp.GetToolsAsync(cancellationToken);

        if (IsExplicitDeposit(userMessage, out var depositAccountId, out var depositAmount))
        {
            var depositTool = ResolveToolName("deposit", tools);
            if (depositTool is not null)
            {
                return new AgentResponse
                {
                    Answer = await _mcp.CallToolAsync(depositTool, new Dictionary<string, JsonElement>
                    {
                        ["accountId"] = JsonSerializer.SerializeToElement(depositAccountId),
                        ["amount"] = JsonSerializer.SerializeToElement(depositAmount)
                    }, cancellationToken)
                };
            }
        }

        var plan = await _model.CreatePlanAsync(userMessage.Trim(), tools, cancellationToken);

        if (string.IsNullOrWhiteSpace(plan.Tool))
        {
            return new AgentResponse
            {
                Answer = new
                {
                    status = "ok",
                    tools,
                    message = plan.Message ?? "The model could not identify a banking action."
                }
            };
        }

        var tool = ResolveToolName(GetRequestedTool(userMessage, plan.Tool), tools);
        if (tool is null)
        {
            return new AgentResponse
            {
                Answer = new
                {
                    status = "invalid_model_plan",
                    message = $"The model selected an unavailable MCP tool: {plan.Tool}",
                    tools
                }
            };
        }

        var arguments = NormalizeArguments(tool, plan.Arguments);
        ApplyUserSuppliedValues(userMessage, tool, arguments);
        var missingArgument = GetRequiredArguments(tool)
            .FirstOrDefault(argument => !arguments.ContainsKey(argument));

        if (missingArgument is not null)
        {
            return new AgentResponse
            {
                Answer = new
                {
                    status = "missing_argument",
                    message = GetMissingArgumentMessage(tool, missingArgument)
                }
            };
        }

        if (RequiresAccountIdentifier(tool) && !ContainsAccountIdentifier(userMessage))
        {
            return new AgentResponse
            {
                Answer = new
                {
                    status = "missing_argument",
                    message = GetMissingArgumentMessage(tool, tool == "get_customer_profile" ? "customerId" : "accountId")
                }
            };
        }

        return new AgentResponse
        {
            Answer = await _mcp.CallToolAsync(tool, arguments, cancellationToken)
        };
    }

    private static string? ResolveToolName(string requestedTool, IReadOnlyList<string> tools)
    {
        var normalized = requestedTool.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return tools.FirstOrDefault(tool =>
            tool.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant() == normalized);
    }

    private static Dictionary<string, JsonElement> NormalizeArguments(
        string tool,
        IReadOnlyDictionary<string, JsonElement> arguments)
    {
        var expected = tool switch
        {
            "create_account" => new[] { "customerName", "initialDeposit" },
            "get_account_balance" => new[] { "accountId" },
            "deposit" => new[] { "accountId", "amount" },
            "withdraw" => new[] { "accountId", "amount" },
            "get_transactions" => new[] { "accountId", "count" },
            "get_customer_profile" => new[] { "customerId" },
            "get_account_status" => new[] { "accountId" },
            _ => []
        };

        return expected
            .Select(name => new
            {
                Name = name,
                Match = arguments.FirstOrDefault(pair =>
                    string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Replace("_", string.Empty, StringComparison.Ordinal)
                        .Equals(name, StringComparison.OrdinalIgnoreCase))
            })
            .Where(item => item.Match.Key is not null && item.Match.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            .ToDictionary(item => item.Name, item => item.Match.Value);
    }

    private static string[] GetRequiredArguments(string tool)
    {
        return tool switch
        {
            "create_account" => ["customerName"],
            "get_account_balance" => ["accountId"],
            "deposit" => ["accountId", "amount"],
            "withdraw" => ["accountId", "amount"],
            "get_transactions" => ["accountId"],
            "get_customer_profile" => ["customerId"],
            "get_account_status" => ["accountId"],
            _ => []
        };
    }

    private static string GetMissingArgumentMessage(string tool, string argument)
    {
        return argument switch
        {
            "accountId" => "Please provide your account ID or account number so I can look that up.",
            "customerId" => "Please provide your customer ID so I can look up your profile.",
            "customerName" => "Please provide the customer name for the new account.",
            "amount" => $"Please provide the amount for the {tool.Replace("_", " ", StringComparison.Ordinal)} request.",
            _ => $"Please provide {argument} to continue."
        };
    }

    private static bool RequiresAccountIdentifier(string tool)
    {
        return tool is "get_account_balance" or "deposit" or "withdraw" or "get_transactions" or "get_account_status" or "get_customer_profile";
    }

    private static bool ContainsAccountIdentifier(string message)
    {
        return TryGetAccountIdentifier(message) is not null;
    }

    private static string GetRequestedTool(string message, string plannedTool)
    {
        if (Regex.IsMatch(message, @"\b(add|deposit|credit|top up|put)\b", RegexOptions.IgnoreCase))
            return "deposit";

        return plannedTool;
    }

    private static void ApplyUserSuppliedValues(
        string message,
        string tool,
        Dictionary<string, JsonElement> arguments)
    {
        var accountIdentifier = TryGetAccountIdentifier(message);
        if (accountIdentifier is not null && RequiresAccountIdentifier(tool))
        {
            arguments[tool == "get_customer_profile" ? "customerId" : "accountId"] =
                JsonSerializer.SerializeToElement(accountIdentifier);
        }

        if (tool is "deposit" or "withdraw")
        {
            var amount = TryGetAmount(message);
            if (amount.HasValue)
                arguments["amount"] = JsonSerializer.SerializeToElement(amount.Value);
        }
    }

    private static string? TryGetAccountIdentifier(string message)
    {
        var match = Regex.Match(message, @"\bAC[A-Za-z0-9]{10}\b|\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}\b");
        return match.Success ? match.Value : null;
    }

    private static decimal? TryGetAmount(string message)
    {
        var amountText = Regex.Replace(
            message,
            @"\bAC[A-Za-z0-9]{10}\b|\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}\b",
            string.Empty);
        var matches = Regex.Matches(amountText, @"(?<![A-Za-z])\$?\d+(?:\.\d{1,2})?\b");
        foreach (Match match in matches)
        {
            if (decimal.TryParse(match.Value.TrimStart('$'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                return amount;
        }

        return null;
    }

    private static bool IsExplicitDeposit(string message, out string accountId, out decimal amount)
    {
        accountId = TryGetAccountIdentifier(message) ?? string.Empty;
        amount = TryGetAmount(message) ?? 0;
        return Regex.IsMatch(message, @"\b(add|deposit|credit|top up|put)\b", RegexOptions.IgnoreCase)
            && accountId.Length > 0
            && amount > 0;
    }
}
