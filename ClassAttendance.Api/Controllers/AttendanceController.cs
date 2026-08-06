using ClassAttendance.Api.Authorization;
using ClassAttendance.Api.Contracts.Attendance;
using ClassAttendance.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("attendance")]
[Authorize(Policy = AuthorizationPolicies.Student)]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<ActionResult> CheckIn(
        CheckInRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await attendanceService.CheckIn(
                GetUserId(),
                request.AttendanceSessionId,
                cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");
}