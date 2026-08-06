using ClassAttendance.Application.Dtos.Auth;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ClassAttendance.Infrastructure.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly DataContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public AuthenticationService(DataContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult?> AuthenticateStudentAsync(
        string index,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedIndex = index.Trim().ToUpperInvariant();
        var student = await _dbContext.Students
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate => candidate.Index.ToUpper() == normalizedIndex
                    && candidate.User.IsActive,
                cancellationToken);

        if (student is null || !PasswordHasher.VerifyPassword(password, student.User.Password))
        {
            return null;
        }

        return _jwtTokenService.CreateToken(student.User);
    }

    public async Task<LoginResult?> AuthenticateProfessorAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail
                && candidate.Role == "Professor"
                && candidate.IsActive,
            cancellationToken);

        if (user is null || !PasswordHasher.VerifyPassword(password, user.Password))
        {
            return null;
        }

        return _jwtTokenService.CreateToken(user);
    }
}
