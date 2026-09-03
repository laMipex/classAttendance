using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Chat;

public sealed class ChatMessageRequest
{
    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;
}
