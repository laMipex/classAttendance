using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Dtos.Auth;

public sealed record LoginResult(
    string Token,
    DateTime Expiration,
    int UserId,
    string FirstName,
    string Email,
    string Role,
    bool TwoFactorEnabled
);

public sealed record TwoFactorSetupResult(string Secret, string AuthenticatorUri);
