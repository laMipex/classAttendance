using ClassAttendance.Api.Contracts.Chat;
using ClassAttendance.Api.Services;
using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Authorize]
[Route("chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly GeminiChatService _geminiChatService;
    private readonly DataContext _dbContext;
    private readonly IScheduleService _scheduleService;

    public ChatbotController(
        GeminiChatService geminiChatService,
        DataContext dbContext,
        IScheduleService scheduleService)
    {
        _geminiChatService = geminiChatService;
        _dbContext = dbContext;
        _scheduleService = scheduleService;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatMessageResponse>> Message(
        ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user id is missing."));
        var isStudent = await _dbContext.Students.AnyAsync(
            student => student.UserId == userId,
            cancellationToken);
        var message = request.Message.Trim();
        var personalAnswer = await TryAnswerPersonalQuestionAsync(message, userId, isStudent, cancellationToken);
        if (personalAnswer is not null)
        {
            return Ok(new ChatMessageResponse(personalAnswer));
        }
        var context = await BuildUserContextAsync(userId, isStudent, cancellationToken);
        var response = await _geminiChatService.GenerateAsync(message, context, cancellationToken);
        return Ok(new ChatMessageResponse(response));
    }

    private async Task<string?> TryAnswerPersonalQuestionAsync(
        string message,
        int userId,
        bool isStudent,
        CancellationToken cancellationToken)
    {
        var normalized = message.ToLowerInvariant();
        var asksAboutNextLecture = normalized.Contains("next lecture")
            || normalized.Contains("next class")
            || normalized.Contains("sledeći čas")
            || normalized.Contains("sledeci cas")
            || normalized.Contains("naredni čas")
            || normalized.Contains("sledeće predavanje")
            || normalized.Contains("sledece predavanje")
            || normalized.Contains("naredno predavanje")
            || normalized.Contains("sledećeg predavanja")
            || normalized.Contains("sledeceg predavanja");
        var asksAboutToday = normalized.Contains("what lecture do i have today")
            || normalized.Contains("lectures today")
            || normalized.Contains("classes today")
            || normalized.Contains("today's lecture")
            || normalized.Contains("današnji čas")
            || normalized.Contains("danasnji cas")
            || normalized.Contains("čas danas")
            || normalized.Contains("cas danas");
        var asksAboutLecturer = normalized.Contains("who") && (normalized.Contains("lecture") || normalized.Contains("class"))
            || normalized.Contains("ko drži")
            || normalized.Contains("profesor");
        if (asksAboutToday || asksAboutNextLecture || asksAboutLecturer)
        {
            var now = DateTime.UtcNow;
            var lookupFrom = new DateTime(2000, 1, 1);
            var lookupUntil = new DateTime(2100, 1, 1);
            var lectures = isStudent
                ? await _scheduleService.GetStudentSchedule(userId, lookupFrom, lookupUntil, cancellationToken)
                : await _scheduleService.GetProfessorSchedule(userId, lookupFrom, lookupUntil, cancellationToken);
            if (asksAboutToday)
            {
                var today = DateTime.Now.Date;
                var todayLectures = lectures
                    .Where(lecture => AsUtc(lecture.StartsAt).ToLocalTime().Date == today)
                    .OrderBy(lecture => lecture.StartsAt)
                    .ToList();
                return todayLectures.Count == 0
                    ? "You have no lectures scheduled for today."
                    : $"Today's lectures:\n{string.Join("\n", todayLectures.Select(lecture =>
                        $"- **{lecture.SubjectName}** at {AsUtc(lecture.StartsAt).ToLocalTime():HH:mm}, room {lecture.Room ?? "not specified"}"))}";
            }

            var nextLecture = lectures
                .Where(lecture => AsUtc(lecture.StartsAt) >= now)
                .OrderBy(lecture => lecture.StartsAt)
                .FirstOrDefault();
            if (nextLecture is null)
            {
                return "There are no lectures in the next eight days.";
            }

            return asksAboutLecturer
                ? $"The next lecture is {nextLecture.SubjectName} at {AsUtc(nextLecture.StartsAt).ToLocalTime():dd.MM.yyyy. HH:mm}. The lecturer is {nextLecture.ProfessorName}."
                : $"The next lecture is {nextLecture.SubjectName}, at {AsUtc(nextLecture.StartsAt).ToLocalTime():dd.MM.yyyy. HH:mm}, room {nextLecture.Room ?? "not specified"}.";
        }

        if (isStudent && (normalized.Contains("quiz") || normalized.Contains("kviz") || normalized.Contains("odgovorio")))
        {
            var quizzes = await _dbContext.QuizSessions
                .Where(quiz => quiz.isActive
                    && !_dbContext.QuizResponses.Any(response =>
                        response.QuizSessionId == quiz.Id && response.StudentId == userId))
                .OrderBy(quiz => quiz.StartsAt)
                .Take(10)
                .Select(quiz => new { quiz.Title, Subject = quiz.Lecture.Subject.Name })
                .ToListAsync(cancellationToken);
            return quizzes.Count == 0
                ? "There are no active quizzes you haven't answered."
                : $"You have {quizzes.Count} active quizzes without answers: {string.Join(", ", quizzes.Select(quiz => $"{quiz.Title} ({quiz.Subject})"))}.";
        }

        return null;
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private async Task<string> BuildUserContextAsync(
        int userId,
        bool isStudent,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scheduleFrom = now.Date;
        var scheduleUntil = scheduleFrom.AddDays(8);
        var lectures = isStudent
            ? await _scheduleService.GetStudentSchedule(userId, scheduleFrom, scheduleUntil, cancellationToken)
            : await _scheduleService.GetProfessorSchedule(userId, scheduleFrom, scheduleUntil, cancellationToken);

        var quizzes = new List<QuizContextItem>();
        if (isStudent)
        {
            quizzes = await _dbContext.QuizSessions
                .Where(quiz => quiz.isActive
                    && !_dbContext.QuizResponses.Any(response =>
                        response.QuizSessionId == quiz.Id && response.StudentId == userId))
                .OrderBy(quiz => quiz.StartsAt)
                .Take(10)
                .Select(quiz => new QuizContextItem(
                    quiz.Title,
                    quiz.Lecture.Subject.Name,
                    quiz.StartsAt,
                    quiz.EndsAt))
                .ToListAsync(cancellationToken);
        }

        var lines = new List<string>
        {
            $"Current UTC time: {now:O}",
            "Upcoming lectures:"
        };
        lines.AddRange(lectures.Take(5).Select(lecture =>
            $"- {lecture.SubjectName}, {lecture.StartsAt:O}, room: {lecture.Room ?? "not specified"}"));
        lines.Add("Unanswered active quizzes:");
        lines.AddRange(quizzes.Select(quiz =>
            $"- {quiz.Title} ({quiz.Subject}), from {quiz.StartsAt:O} until {quiz.EndsAt:O}"));
        if (isStudent)
        {
            var lessonQuestions = await _dbContext.QuizQuestions
                .Where(question => question.QuizSession.isActive)
                .OrderBy(question => question.QuizSession.StartsAt)
                .Take(30)
                .Select(question => question.Text)
                .ToListAsync(cancellationToken);
            lines.Add("Available lesson topics from active quizzes:");
            lines.AddRange(lessonQuestions.Select(question => $"- {question}"));
        }
        return string.Join(Environment.NewLine, lines);
    }

    private sealed record QuizContextItem(
        string Title,
        string Subject,
        DateTime StartsAt,
        DateTime EndsAt);
}

public sealed record ChatMessageResponse(string Message);
