using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace AI_Agent.Services;

public sealed class McpClientService
{
    private readonly HttpClient _httpClient;

    public McpClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        await using var client = await CreateClientAsync(cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(tool => tool.Name).ToArray();
    }

    public async Task<object> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        await using var client = await CreateClientAsync(cancellationToken);
        var toolArguments = arguments.ToDictionary(
            pair => pair.Key,
            pair => ConvertJsonElement(pair.Value));
        var result = await client.CallToolAsync(toolName, toolArguments, cancellationToken: cancellationToken);

        if (result.StructuredContent is not null)
        {
            return result.StructuredContent;
        }

        if (result.Content.Count == 1 && result.Content[0] is TextContentBlock text)
        {
            try
            {
                return JsonSerializer.Deserialize<object>(text.Text) ?? text.Text;
            }
            catch (JsonException)
            {
                return text.Text;
            }
        }

        var serialized = JsonSerializer.Serialize(result);
        return JsonSerializer.Deserialize<object>(serialized) ?? new { success = true };
    }

    private async Task<McpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var endpoint = new Uri(_httpClient.BaseAddress!, "mcp");
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint
        }, _httpClient, ownsHttpClient: false);

        return await McpClient.CreateAsync(clientTransport: transport, cancellationToken: cancellationToken);
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertJsonElement(property.Value)),
            JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonElement).ToList(),
            _ => null
        };
    }
}
