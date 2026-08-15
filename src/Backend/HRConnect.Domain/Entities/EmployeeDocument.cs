namespace HRConnect.Domain.Entities;

public sealed class EmployeeDocument
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Notes { get; set; }
    public int UploadedByUserAccountId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public Employee Employee { get; set; } = null!;
    public UserAccount UploadedByUserAccount { get; set; } = null!;
}
