using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Dtos.Auth;

public sealed record LoginResult(
    string Token,
    DateTime Expiration,
    int UserId,
    string Email,
    string Role
);


