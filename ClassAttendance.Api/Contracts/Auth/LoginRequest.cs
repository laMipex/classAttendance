using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Auth;

public sealed class LoginRequest : IValidatableObject
{
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Index must contain exactly eight digits.")]
    public string? Index { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasIndex = !string.IsNullOrWhiteSpace(Index);
        var hasEmail = !string.IsNullOrWhiteSpace(Email);

        if(hasEmail == hasIndex)
        {
            yield return new ValidationResult(
                "Provide either index for a student or email for a professor!",
                [nameof(Index), nameof(Email)]);
        }
    }
}


