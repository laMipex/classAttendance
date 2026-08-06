using ClassAttendance.Api.Authorization;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("professor")]
[Authorize(Policy = AuthorizationPolicies.Professor)]
public sealed class ProfessorController(IAttendanceService attendanceService) : ControllerBase
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

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user identifier is missing.");
}
