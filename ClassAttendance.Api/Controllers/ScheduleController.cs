using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassAttendance.Api.Authorization;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.Student)]
public class ScheduleController : ControllerBase
{
    // TODO: Implement schedule endpoints
}
