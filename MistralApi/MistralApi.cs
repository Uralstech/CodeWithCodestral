using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using JsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace CodeWithCodestral.MistralApi;

#pragma warning disable CS8618

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class MistralChatCompletionRequest
{
    public string Model = "codestral-latest";
    public bool Stream = false;
    public int MaxTokens = 1024;

    public List<MistralChatMessage> Messages = new();
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class MistralChatMessage
{
    public MistralChatMessageRole Role;
    public string Content;
}

[JsonConverter(typeof(StringEnumConverter), [typeof(SnakeCaseNamingStrategy)])]
public enum MistralChatMessageRole
{
    System,
    Assistant,
    User,
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class MistralChatResponse
{
    public string Id;
    public string Object;
    public long Created;
    public string Model;
    public MistralChatChoice[] Choices;
    public MistralChatUsage Usage;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class MistralChatChoice
{
    public int Index;
    public MistralChatMessage Message;
    public string FinishReason;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class MistralChatUsage
{
    public int PromptTokens;
    public int CompletionTokens;
    public int TotalTokens;
}

#pragma warning restore CS8618

public static class MistralApi
{
    private static readonly HttpClient s_httpClient = new();

    public static string ApiEndpoint = "https://api.mistral.ai/v1";

    private static string s_apiKey = string.Empty;
    public static string ApiKey
    {
        get => s_apiKey;
        set
        {
            s_apiKey = value;
            s_httpClient.DefaultRequestHeaders.Authorization = new("Bearer", value);
        }
    }
    
    public static async Task<MistralChatResponse?> ChatCompletion(MistralChatCompletionRequest request)
    {
        string requestJson = JsonConvert.SerializeObject(request);

        try
        {
            using HttpResponseMessage response = await s_httpClient.PostAsync(
                $"{ApiEndpoint}/chat/completions",
                new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            
            string responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MistralChatResponse>(responseJson);
        }
        catch (HttpRequestException exception)
        {
            Debug.WriteLine($"Failed Mistral API request: {exception.Message}");
            return null;
        }
    }

    public static MistralChatMessage? ExtractChatResponse(MistralChatResponse? response)
    {
        return response?.Choices.First().Message;
    }
}
