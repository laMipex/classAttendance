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
                dbContext.QuizQuestions.Count(question => question.QuizSessionId == quiz.Id),
                quiz.isActive))
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
                dbContext.QuizQuestions.Count(question => question.QuizSessionId == quiz.Id),
                quiz.isActive))
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

        if (request.QuestionType == "Choice" && request.Options.Count < 2)
        {
            return BadRequest(new { error = "Choice questions require at least two options." });
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

        var question = new Domain.Entities.QuizQuestion
        {
            QuizSessionId = quiz.Id,
            Text = request.Question.Trim(),
            Type = request.QuestionType,
            OrderNo = 1
        };
        dbContext.QuizQuestions.Add(question);
        if (request.QuestionType == "Choice")
        {
            dbContext.QuizOptions.AddRange(request.Options.Select(option => new Domain.Entities.QuizOption
            {
                Question = question,
                Text = option.Text.Trim(),
                IsCorrect = option.IsCorrect
            }));
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMine), new QuizSummaryResponse(
            quiz.Id,
            quiz.LectureId,
            quiz.Title,
            lecture.Subject.Name,
            quiz.StartsAt,
            quiz.EndsAt,
            1,
            quiz.isActive));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.Student)]
    public async Task<ActionResult<QuizDetailsResponse>> GetDetails(int id, CancellationToken cancellationToken)
    {
        var studentId = GetUserId();
        var quiz = await dbContext.QuizSessions
            .Where(candidate => candidate.Id == id && candidate.isActive
                && dbContext.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId
                    && enrollment.SubjectId == candidate.Lecture.SubjectId
                    && enrollment.Status == "Active"))
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Title
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var questions = await dbContext.QuizQuestions
            .Where(question => question.QuizSessionId == id)
            .OrderBy(question => question.OrderNo)
            .Select(question => new QuizQuestionResponse(
                question.Id,
                question.Text,
                question.Type,
                dbContext.QuizOptions
                    .Where(option => option.QuestionId == question.Id)
                    .Select(option => new QuizOptionResponse(option.Id, option.Text))
                    .ToList(),
                dbContext.QuizResponses
                    .Where(response => response.QuestionId == question.Id && response.StudentId == studentId)
                    .Select(response => response.AnswerText)
                    .SingleOrDefault(),
                dbContext.QuizResponses
                    .Where(response => response.QuestionId == question.Id && response.StudentId == studentId)
                    .Select(response => response.SelectedOptionId)
                    .SingleOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(new QuizDetailsResponse(quiz.Id, quiz.Title, questions));
    }

    [HttpGet("{id:int}/responses")]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<ActionResult<QuizResponsesResponse>> GetResponses(int id, CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var quiz = await dbContext.QuizSessions
            .Where(candidate => candidate.Id == id && candidate.Lecture.ProfessorId == professorId)
            .Select(candidate => new { candidate.Id, candidate.Title })
            .SingleOrDefaultAsync(cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var questions = await dbContext.QuizQuestions
            .Where(question => question.QuizSessionId == id)
            .OrderBy(question => question.OrderNo)
            .Select(question => new QuizQuestionResponsesResponse(
                question.Id,
                question.Text,
                dbContext.QuizResponses
                    .Where(response => response.QuestionId == question.Id)
                    .OrderBy(response => response.Student.Index)
                    .Select(response => new QuizStudentResponse(
                        response.StudentId,
                        response.Student.Index,
                        response.AnswerText,
                        response.SelectedOptionId,
                        dbContext.QuizOptions
                            .Where(option => option.Id == response.SelectedOptionId)
                            .Select(option => option.Text)
                            .SingleOrDefault(),
                        response.SubmittedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(new QuizResponsesResponse(quiz.Id, quiz.Title, questions));
    }

    [HttpPost("{id:int}/responses")]
    [Authorize(Policy = AuthorizationPolicies.Student)]
    public async Task<IActionResult> Submit(int id, SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var studentId = GetUserId();
        var quiz = await dbContext.QuizSessions
            .Where(candidate => candidate.Id == id && candidate.isActive
                && dbContext.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId
                    && enrollment.SubjectId == candidate.Lecture.SubjectId
                    && enrollment.Status == "Active"))
            .SingleOrDefaultAsync(cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var questions = await dbContext.QuizQuestions
            .Where(question => question.QuizSessionId == id)
            .ToListAsync(cancellationToken);
        if (request.Answers.Count != questions.Count
            || request.Answers.Select(answer => answer.QuestionId).Distinct().Count() != questions.Count
            || request.Answers.Any(answer => questions.All(question => question.Id != answer.QuestionId)))
        {
            return BadRequest(new { error = "An answer is required for every quiz question." });
        }

        foreach (var answer in request.Answers)
        {
            var question = questions.Single(question => question.Id == answer.QuestionId);
            if (question.Type == "Choice")
            {
                if (answer.SelectedOptionId is null
                    || !await dbContext.QuizOptions.AnyAsync(option =>
                        option.Id == answer.SelectedOptionId && option.QuestionId == question.Id, cancellationToken))
                {
                    return BadRequest(new { error = $"Select an option for question {question.Id}." });
                }
            }
            else if (string.IsNullOrWhiteSpace(answer.AnswerText))
            {
                return BadRequest(new { error = $"Write an answer for question {question.Id}." });
            }
        }

        var existing = await dbContext.QuizResponses
            .Where(response => response.QuizSessionId == id && response.StudentId == studentId)
            .ToDictionaryAsync(response => response.QuestionId, cancellationToken);
        foreach (var answer in request.Answers)
        {
            if (!existing.TryGetValue(answer.QuestionId, out var response))
            {
                response = new Domain.Entities.QuizResponse
                {
                    QuizSessionId = id,
                    QuestionId = answer.QuestionId,
                    StudentId = studentId
                };
                dbContext.QuizResponses.Add(response);
            }

            response.AnswerText = answer.AnswerText?.Trim();
            response.SelectedOptionId = answer.SelectedOptionId;
            response.SubmittedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/visibility")]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<IActionResult> SetVisibility(int id, SetQuizVisibilityRequest request, CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var quiz = await dbContext.QuizSessions
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.Lecture.ProfessorId == professorId, cancellationToken);
        if (quiz is null)
        {
            return NotFound();
        }

        quiz.isActive = request.IsVisible;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.Professor)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var professorId = GetUserId();
        var quiz = await dbContext.QuizSessions
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.Lecture.ProfessorId == professorId, cancellationToken);
        if (quiz is null)
        {
            return NotFound();
        }

        dbContext.QuizResponses.RemoveRange(dbContext.QuizResponses.Where(response => response.QuizSessionId == id));
        dbContext.QuizSessions.Remove(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");
}