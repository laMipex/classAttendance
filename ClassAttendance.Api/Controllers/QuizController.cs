using ClassAttendance.Api.Authorization;
using ClassAttendance.Api.Contracts.Quiz;
using ClassAttendance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("quiz")]
[Authorize]
public sealed class QuizController(DataContext dbContext) : ControllerBase
{
    [HttpGet("available")]
    [Authorize(Policy = AuthorizationPolicies.Student)]
    public async Task<ActionResult<IReadOnlyList<QuizSummaryResponse>>> GetAvailable(CancellationToken cancellationToken)
    {
        var studentId = GetUserId();
        var quizzes = await dbContext.QuizSessions
            .Where(quiz => quiz.isActive
                && dbContext.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId
                    && enrollment.SubjectId == quiz.Lecture.SubjectId
                    && enrollment.Status == "Active"))
            .OrderBy(quiz => quiz.EndsAt)
            .Select(quiz => new QuizSummaryResponse(
                quiz.Id,
                quiz.LectureId,
                quiz.Title,
                quiz.Lecture.Subject.Name,
                quiz.StartsAt,
                quiz.EndsAt,
                dbContext.QuizQuestions.Count(question => question.QuizSessionId == quiz.Id)))
            .ToListAsync(cancellationToken);

        return Ok(quizzes);
    }

    [HttpGet("mine")]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<ActionResult<IReadOnlyList<QuizSummaryResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var quizzes = await dbContext.QuizSessions
            .Where(quiz => quiz.Lecture.ProfessorId == professorId)
            .OrderByDescending(quiz => quiz.StartsAt)
            .Select(quiz => new QuizSummaryResponse(
                quiz.Id,
                quiz.LectureId,
                quiz.Title,
                quiz.Lecture.Subject.Name,
                quiz.StartsAt,
                quiz.EndsAt,
                dbContext.QuizQuestions.Count(question => question.QuizSessionId == quiz.Id)))
            .ToListAsync(cancellationToken);

        return Ok(quizzes);
    }

    [HttpGet("lectures")]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<ActionResult<IReadOnlyList<QuizLectureResponse>>> GetLectures(CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var lectures = await dbContext.Lectures
            .Where(lecture => lecture.ProfessorId == professorId && lecture.EndsAt >= DateTime.UtcNow)
            .OrderBy(lecture => lecture.StartsAt)
            .Select(lecture => new QuizLectureResponse(
                lecture.Id,
                lecture.Subject.Name,
                lecture.StartsAt,
                lecture.EndsAt,
                lecture.Rooms))
            .ToListAsync(cancellationToken);

        return Ok(lectures);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<ActionResult<QuizSummaryResponse>> Create(
        CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var lecture = await dbContext.Lectures
            .Include(candidate => candidate.Subject)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.LectureId && candidate.ProfessorId == professorId,
                cancellationToken);

        if (lecture is null)
        {
            return NotFound(new { error = "The selected lecture was not found." });
        }

        var quiz = new Domain.Entities.QuizSession
        {
            LectureId = lecture.Id,
            Title = request.Title.Trim(),
            StartsAt = lecture.StartsAt,
            EndsAt = lecture.EndsAt,
            isActive = true
        };
        dbContext.QuizSessions.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.QuizQuestions.Add(new Domain.Entities.QuizQuestion
        {
            QuizSessionId = quiz.Id,
            Text = request.Question.Trim(),
            Type = "Text",
            OrderNo = 1
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMine), new QuizSummaryResponse(
            quiz.Id,
            quiz.LectureId,
            quiz.Title,
            lecture.Subject.Name,
            quiz.StartsAt,
            quiz.EndsAt,
            1));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");
}