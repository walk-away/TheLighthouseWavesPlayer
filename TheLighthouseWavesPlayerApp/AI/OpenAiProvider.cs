using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;

namespace TheLighthouseWavesPlayerApp.AI;

public class OpenAiProvider : IAiProvider
{
    private readonly ChatClient _chatClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public OpenAiProvider()
    {
        var apiKey = AiSettings.ApiKey;
        _chatClient = new ChatClient(model: "gpt-4o", apiKey: apiKey);
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string?> GetResponse(string request)
    {
        try
        {
            var completion = await _chatClient.CompleteChatAsync(request);
            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}