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
            system_instruction = new { parts = new[] { new { text = "You are the ClassAttendance assistant. Help the user with attendance, schedules, quizzes, account login, and other general questions. For personal questions about the user's schedule, quizzes, or attendance, use the supplied user context and never invent application data; if the context does not contain the answer, say so. For general educational or everyday questions, answer normally with a concrete, useful explanation and examples. Reply in the language used by the user." } } },
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
        if (!document.RootElement.TryGetProperty("candidates", out var candidates))
        {
            throw new InvalidOperationException("Gemini returned no candidates.");
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content)
                || !content.TryGetProperty("parts", out var parts))
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text)
                    && text.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    return text.GetString()!;
                }
            }
        }

        string? finishReason = null;
        if (candidates.GetArrayLength() > 0
            && candidates[0].TryGetProperty("finishReason", out var reason)
            && reason.ValueKind == JsonValueKind.String)
        {
            finishReason = reason.GetString();
        }
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(finishReason)
                ? "Gemini returned no text."
                : $"Gemini returned no text (finish reason: {finishReason}).");
    }
}
