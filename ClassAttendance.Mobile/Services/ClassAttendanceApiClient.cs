using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClassAttendance.Mobile.Services;

public sealed class ClassAttendanceApiClient
{
    private readonly HttpClient _httpClient;

    public ClassAttendanceApiClient()
    {
        var baseAddress = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5228/"
            : "http://localhost:5228/";

        _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public async Task<LoginResponse?> LoginAsync(string? index, string? email, string password, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "auth/login",
            new LoginRequest(index, email, password),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
    }

    public void SetAuthorization(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<IReadOnlyList<QuizResponse>> GetStudentQuizzesAsync(CancellationToken cancellationToken) =>
        await GetAsync<List<QuizResponse>>("quiz/available", cancellationToken);

    public async Task<IReadOnlyList<QuizResponse>> GetProfessorQuizzesAsync(CancellationToken cancellationToken) =>
        await GetAsync<List<QuizResponse>>("quiz/mine", cancellationToken);

    public async Task<IReadOnlyList<LectureResponse>> GetProfessorLecturesAsync(CancellationToken cancellationToken) =>
        await GetAsync<List<LectureResponse>>("quiz/lectures", cancellationToken);

    public async Task<QuizResponse> CreateQuizAsync(CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("quiz", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuizResponse>(cancellationToken))!;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken))!;
    }
}

public sealed record LoginRequest(string? Index, string? Email, string Password);
public sealed record LoginResponse(string Token, DateTime Expiration, int UserId, string FirstName, string Email, string Role);
public sealed record QuizResponse(int Id, int LectureId, string Title, string SubjectName, DateTime StartsAt, DateTime EndsAt, int QuestionCount);
public sealed record LectureResponse(int Id, string SubjectName, DateTime StartsAt, DateTime EndsAt, string? Room)
{
    public string DisplayName => $"{SubjectName} · {StartsAt:ddd, dd MMM HH:mm}";
}
public sealed record CreateQuizRequest(int LectureId, string Title, string Question);
