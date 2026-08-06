using ClassAttendance.Api.Authorization;
using ClassAttendance.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("schedule")]
[Authorize(Policy = AuthorizationPolicies.Student)]
public sealed class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> GetMySchedule(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? until,
        CancellationToken cancellationToken)
    {
        var start = from ?? DateTime.UtcNow.Date;
        var end = until ?? start.AddDays(7);

        if (end <= start)
        {
            return BadRequest(new { error = "The schedule end must be after its start." });
        }

        var schedule = await scheduleService.GetStudentSchedule(
            GetUserId(),
            start,
            end,
            cancellationToken);

        return Ok(schedule);
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");
}