using ClassAttendance.Api.Authorization;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Api.Contracts.Schedule;
using ClassAttendance.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("professor")]
[Authorize(Policy = AuthorizationPolicies.Professor)]
public sealed class ProfessorController(
    IAttendanceService attendanceService,
    IScheduleService scheduleService) : ControllerBase
{
    [HttpGet("subjects/{id:int}/attendances")]
    public async Task<ActionResult> GetSubjectAttendances(int id, CancellationToken cancellationToken)
    {
        try
        {
            var attendances = await attendanceService.GetForProfessorSubject(
                GetUserId(),
                id,
                cancellationToken);
            return Ok(attendances);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("schedule")]
    public async Task<ActionResult> GetSchedule(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? until,
        CancellationToken cancellationToken)
    {
        var start = from ?? StartOfScheduleWeek(DateTime.UtcNow);
        var end = until ?? start.AddDays(7);
        if (end <= start)
        {
            return BadRequest(new { error = "The schedule end must be after its start." });
        }

        return Ok(await scheduleService.GetProfessorSchedule(
            GetUserId(), start, end, cancellationToken));
    }

    [HttpPut("schedule/{id:int}")]
    public async Task<ActionResult> UpdateSchedule(
        int id,
        UpdateLectureRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await scheduleService.UpdateLecture(
                GetUserId(), id, request.StartsAt, request.EndsAt, request.Room, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");

    private static DateTime StartOfScheduleWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var start = date.Date.AddDays(-daysSinceMonday);
        return start;
    }
}
