using ClassAttendance.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Application.Dtos.Auth;

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
                request.TwoFactorCode,
                cancellationToken)
            : await _authenticationService.AuthenticateProfessorAsync(
                request.Email!,
                request.Password,
                request.TwoFactorCode,
                cancellationToken);
        return loginResult is null ? Unauthorized() : Ok(loginResult);
    }

    [Authorize]
    [HttpPost("two-factor/setup")]
    public Task<TwoFactorSetupResult> SetupTwoFactor(CancellationToken cancellationToken) =>
        _authenticationService.BeginTwoFactorSetupAsync(CurrentUserId(), cancellationToken);

    [Authorize]
    [HttpPost("two-factor/enable")]
    public async Task<ActionResult> EnableTwoFactor([FromBody] TwoFactorCodeRequest request, CancellationToken cancellationToken) =>
        await _authenticationService.EnableTwoFactorAsync(CurrentUserId(), request.Code, cancellationToken)
            ? Ok()
            : BadRequest("The authenticator code is invalid.");

    [Authorize]
    [HttpPost("two-factor/disable")]
    public async Task<ActionResult> DisableTwoFactor([FromBody] TwoFactorCodeRequest request, CancellationToken cancellationToken) =>
        await _authenticationService.DisableTwoFactorAsync(CurrentUserId(), request.Code, cancellationToken)
            ? Ok()
            : BadRequest("The authenticator code is invalid.");

    [AllowAnonymous]
    [HttpGet("two-factor/status")]
    public Task<bool> TwoFactorStatus(
        [FromQuery] string? index,
        [FromQuery] string? email,
        CancellationToken cancellationToken) =>
        _authenticationService.IsTwoFactorEnabledAsync(index, email, cancellationToken);

    private int CurrentUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("The authenticated user id is missing."));
}
