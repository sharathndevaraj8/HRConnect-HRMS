using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public sealed class ForgotPasswordRequestDto
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequestDto
{
    [Required, MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(12), MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? DevelopmentResetUrl { get; set; }
}
