using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public sealed class UpdateEmployeeDto : CreateEmployeeDto
{
    [Required] public int Id { get; set; }
}
