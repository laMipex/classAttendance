using System.Net.Http.Json;
using System.Text.Json;

namespace ClassAttendance.Api.Services;

public sealed class GeminiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _configuration = configuration;
    }

    public async Task<string> GenerateAsync(
        string message,
        string userContext,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        }

        var model = _configuration["Gemini:Model"] ?? "gemini-3.6-flash";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var request = new
        {
            system_instruction = new { parts = new[] { new { text = "You are the ClassAttendance assistant. Answer briefly and helpfully about attendance, schedules, quizzes, and account login. Use the supplied user context for personal questions. Never invent schedule or quiz data. If the context does not contain the answer, say so. Reply in the language used by the user." } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = $"User context:\n{userContext}\n\nQuestion:\n{message}" } } } }
        };

        using var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini returned {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}");
        }
        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Gemini returned an empty response.");
    }
}
