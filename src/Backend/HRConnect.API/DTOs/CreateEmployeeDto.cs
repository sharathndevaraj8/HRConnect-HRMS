using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public class CreateEmployeeDto
{
    [Required]
    [MinLength(2)]
    [MaxLength(50)]
    [RegularExpression(@"^[A-Za-z][A-Za-z.'-]*(?: [A-Za-z][A-Za-z.'-]*)*$")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(50)]
    [RegularExpression(@"^[A-Za-z][A-Za-z.'-]*(?: [A-Za-z][A-Za-z.'-]*)*$")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^(Software Engineer|Senior Software Engineer|Frontend Developer|Backend Developer|Full Stack Developer|QA Engineer|DevOps Engineer|Cloud Engineer|Data Engineer|UI/UX Designer|Business Analyst|Project Manager|Scrum Master|System Administrator|Technical Lead)$")]
    public string Designation { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfJoining { get; set; }
}
