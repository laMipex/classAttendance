using ClassAttendance.Application.Dtos.Auth;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Domain.Entities;
using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net;

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
        string? twoFactorCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedIndex = index.Trim().ToUpperInvariant();
        var student = await _dbContext.Students
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate => candidate.Index.ToUpper() == normalizedIndex
                    && candidate.User.IsActive,
                cancellationToken);

        if (student is null || !PasswordHasher.VerifyPassword(password, student.User.Password)
            || !IsTwoFactorValid(student.User, twoFactorCode))
        {
            return null;
        }

        return _jwtTokenService.CreateToken(student.User);
    }

    public async Task<LoginResult?> AuthenticateProfessorAsync(
        string email,
        string password,
        string? twoFactorCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail
                && candidate.Role == "Professor"
                && candidate.IsActive,
            cancellationToken);

        if (user is null || !PasswordHasher.VerifyPassword(password, user.Password)
            || !IsTwoFactorValid(user, twoFactorCode))
        {
            return null;
        }

        return _jwtTokenService.CreateToken(user);
    }

    public async Task<TwoFactorSetupResult> BeginTwoFactorSetupAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        var secret = user.TwoFactorSecret ?? CreateSecret();
        user.TwoFactorSecret = secret;
        await _dbContext.SaveChangesAsync(cancellationToken);
        var label = Uri.EscapeDataString($"ClassAttendance:{user.Email}");
        return new TwoFactorSetupResult(secret, $"otpauth://totp/{label}?secret={secret}&issuer=ClassAttendance");
    }

    public async Task<bool> EnableTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret) || !VerifyCode(user.TwoFactorSecret, code))
        {
            return false;
        }
        user.TwoFactorEnabled = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DisableTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        if (!user.TwoFactorEnabled || user.TwoFactorSecret is null || !VerifyCode(user.TwoFactorSecret, code))
        {
            return false;
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsTwoFactorEnabledAsync(
        string? index,
        string? email,
        CancellationToken cancellationToken = default)
    {
        User? user;
        if (!string.IsNullOrWhiteSpace(index))
        {
            var normalizedIndex = index.Trim().ToUpperInvariant();
            user = await _dbContext.Students
                .Where(student => student.Index.ToUpper() == normalizedIndex)
                .Select(student => student.User)
                .SingleOrDefaultAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            user = await _dbContext.Users
                .SingleOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail
                    && candidate.Role == "Professor", cancellationToken);
        }
        else
        {
            return false;
        }

        return user?.IsActive == true && user.TwoFactorEnabled;
    }

    private static bool IsTwoFactorValid(User user, string? code) =>
        !user.TwoFactorEnabled || (user.TwoFactorSecret is not null && VerifyCode(user.TwoFactorSecret, code));

    private static string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder(32);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                result.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) result.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return result.ToString();
    }

    private static bool VerifyCode(string secret, string? code)
    {
        if (code is null || code.Length != 6 || !int.TryParse(code, out _)) return false;
        var key = DecodeBase32(secret);
        var counter = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30));
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 15;
        var value = ((hash[offset] & 127) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6") == code;
    }

    private static byte[] DecodeBase32(string value)
    {
        var output = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in value)
        {
            buffer = (buffer << 5) | "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(character);
            bits += 5;
            if (bits >= 8) { output.Add((byte)(buffer >> (bits - 8))); bits -= 8; }
        }
        return output.ToArray();
    }
}
