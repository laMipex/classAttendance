using ClassAttendance.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<LoginResult?> AuthenticateStudentAsync(string index, string password, string? twoFactorCode, CancellationToken cancellationToken = default);
    Task<LoginResult?> AuthenticateProfessorAsync(string email, string password, string? twoFactorCode, CancellationToken cancellationToken = default);
    Task<TwoFactorSetupResult> BeginTwoFactorSetupAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> EnableTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default);
    Task<bool> DisableTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default);
    Task<bool> IsTwoFactorEnabledAsync(string? index, string? email, CancellationToken cancellationToken = default);
}
