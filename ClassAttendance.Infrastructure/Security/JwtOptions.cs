using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClassAttendance.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;

    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("JWT Issuer is not configured properly."); 
        }

        if(string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT Audience is not configured properly.");
        }

        if(SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SigningKey must be at least 32 characters long.");
        }

        if(ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT ExpirationMinutes must be greater than 0.");
        }
    }
}
