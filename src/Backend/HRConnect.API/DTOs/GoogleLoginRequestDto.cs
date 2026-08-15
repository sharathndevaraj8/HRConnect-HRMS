using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public sealed class GoogleLoginRequestDto
{
    [Required]
    public string Credential { get; set; } = string.Empty;
}
