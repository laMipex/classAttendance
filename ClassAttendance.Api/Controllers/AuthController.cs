using ClassAttendance.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassAttendance.Application.Interfaces.Services;

namespace ClassAttendance.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var loginResult = !string.IsNullOrWhiteSpace(request.Index)
            ? await _authenticationService.AuthenticateStudentAsync(
                request.Index,
                request.Password,
                cancellationToken)
            : await _authenticationService.AuthenticateProfessorAsync(
                request.Email!,
                request.Password,
                cancellationToken);
        return loginResult is null ? Unauthorized() : Ok(loginResult);
    }
}
