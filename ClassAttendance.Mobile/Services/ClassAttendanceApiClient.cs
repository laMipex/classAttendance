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

    public async Task<IReadOnlyList<ScheduleLectureResponse>> GetProfessorScheduleAsync(
        CancellationToken cancellationToken) =>
        await GetAsync<List<ScheduleLectureResponse>>("professor/schedule", cancellationToken);

    public async Task UpdateLectureAsync(
        int lectureId,
        DateTime startsAt,
        DateTime endsAt,
        string? room,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"professor/schedule/{lectureId}",
            new UpdateLectureRequest(startsAt, endsAt, room),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ScheduleLectureResponse>> GetStudentScheduleAsync(
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken)
    {
        var path = $"schedule/me?from={Uri.EscapeDataString(from.ToString("O"))}&until={Uri.EscapeDataString(until.ToString("O"))}";
        return await GetAsync<List<ScheduleLectureResponse>>(path, cancellationToken);
    }

    public async Task<CheckInResponse> CheckInAsync(
        int attendanceSessionId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "attendance/check-in",
            new CheckInRequest(attendanceSessionId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            if (error.Contains("already checked in", StringComparison.OrdinalIgnoreCase))
            {
                throw new AttendanceAlreadyConfirmedException();
            }
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckInResponse>(cancellationToken))!;
    }

    public async Task<CheckInResponse?> GetMyCheckIn(
        int attendanceSessionId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"attendance/sessions/{attendanceSessionId}/me",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CheckInResponse>(cancellationToken);
    }

    public async Task<QuizResponse> CreateQuizAsync(CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("quiz", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuizResponse>(cancellationToken))!;
    }

    public async Task<QuizDetailsResponse> GetQuizDetailsAsync(int quizId, CancellationToken cancellationToken) =>
        await GetAsync<QuizDetailsResponse>($"quiz/{quizId}", cancellationToken);

    public async Task<QuizResponsesResponse> GetQuizResponsesAsync(int quizId, CancellationToken cancellationToken) =>
        await GetAsync<QuizResponsesResponse>($"quiz/{quizId}/responses", cancellationToken);

    public async Task SubmitQuizAsync(int quizId, SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync($"quiz/{quizId}/responses", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetQuizVisibilityAsync(int quizId, bool isVisible, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"quiz/{quizId}/visibility",
            new SetQuizVisibilityRequest(isVisible),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQuizAsync(int quizId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"quiz/{quizId}", cancellationToken);
        response.EnsureSuccessStatusCode();
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
public sealed record QuizResponse(int Id, int LectureId, string Title, string SubjectName, DateTime StartsAt, DateTime EndsAt, int QuestionCount, bool IsVisible);
public sealed record LectureResponse(int Id, string SubjectName, DateTime StartsAt, DateTime EndsAt, string? Room)
{
    public string DisplayName => $"{SubjectName} · {StartsAt:ddd, dd MMM HH:mm}";
}
public sealed record CreateQuizRequest(int LectureId, string Title, string Question, string QuestionType, IReadOnlyList<CreateQuizOptionRequest> Options);
public sealed record CreateQuizOptionRequest(string Text, bool IsCorrect);
public sealed record QuizDetailsResponse(int Id, string Title, IReadOnlyList<QuizQuestionResponse> Questions);
public sealed record QuizQuestionResponse(int Id, string Text, string Type, IReadOnlyList<QuizOptionResponse> Options, string? AnswerText, int? SelectedOptionId);
public sealed record QuizOptionResponse(int Id, string Text);
public sealed record QuizResponsesResponse(int Id, string Title, IReadOnlyList<QuizQuestionResponsesResponse> Questions);
public sealed record QuizQuestionResponsesResponse(int Id, string Text, IReadOnlyList<QuizStudentResponse> Responses);
public sealed record QuizStudentResponse(int StudentId, string StudentIndex, string? AnswerText, int? SelectedOptionId, string? SelectedOptionText, DateTime SubmittedAt)
{
    public string DisplayAnswer => AnswerText ?? SelectedOptionText ?? "No answer";
};
public sealed record SubmitQuizRequest(IReadOnlyList<QuizAnswerRequest> Answers);
public sealed record QuizAnswerRequest(int QuestionId, string? AnswerText, int? SelectedOptionId);
public sealed record SetQuizVisibilityRequest(bool IsVisible);
public sealed record CheckInRequest(int AttendanceSessionId);
public sealed record CheckInResponse(int AttendanceId, int AttendanceSessionId, DateTime CheckInAt, string Status);
public sealed record ScheduleLectureResponse(
    int Id,
    int SubjectId,
    string SubjectCode,
    string SubjectName,
    string ProfessorName,
    DateTime StartsAt,
    DateTime EndsAt,
    string? Room,
    IReadOnlyList<ScheduleAttendanceSessionResponse> AttendanceSessions);
public sealed record ScheduleAttendanceSessionResponse(int Id, DateTime OpenFrom, DateTime OpenUntil);
public sealed record UpdateLectureRequest(DateTime StartsAt, DateTime EndsAt, string? Room);

public sealed class AttendanceAlreadyConfirmedException : Exception;
