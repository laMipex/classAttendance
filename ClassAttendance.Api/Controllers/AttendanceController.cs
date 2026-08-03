using ClassAttendance.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.Student)]
public class AttendanceController : ControllerBase
{
    // TODO: Implement attendance endpoints
}