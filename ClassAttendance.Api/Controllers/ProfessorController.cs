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
    public async Task<ActionResult> GetSchedule(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return Ok(await scheduleService.GetProfessorSchedule(
            GetUserId(), now.Date, now.Date.AddDays(30), cancellationToken));
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
}
