using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Auth;

public sealed class TwoFactorCodeRequest
{
    [RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = string.Empty;
}
