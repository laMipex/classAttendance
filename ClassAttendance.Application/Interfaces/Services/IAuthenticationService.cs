using ClassAttendance.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<LoginResult?> AuthenticateStudentAsync(string index, string password, CancellationToken cancellationToken = default);
    Task<LoginResult?> AuthenticateProfessorAsync(string email, string password, CancellationToken cancellationToken = default);
}
