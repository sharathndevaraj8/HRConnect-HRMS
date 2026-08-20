using System.Security.Claims;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/{employeeId:int}/documents")]
public sealed class EmployeeDocumentsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "PanCard", "AadhaarCard", "ProfilePhoto", "Passport", "Education", "Employment", "Other" };
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private readonly AppDbContext _db;
    private readonly string _storageRoot;

    public EmployeeDocumentsController(
        AppDbContext db,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _db = db;
        var configuredRoot = configuration["DocumentStorage:RootPath"];
        var appServiceHome = configuration["HOME"];
        var isAzureAppService = !string.IsNullOrWhiteSpace(configuration["WEBSITE_INSTANCE_ID"]);

        _storageRoot = !string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : isAzureAppService && !string.IsNullOrWhiteSpace(appServiceHome)
                ? Path.Combine(appServiceHome, "data", "employee-documents")
                : Path.Combine(environment.ContentRootPath, "App_Data", "employee-documents");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int employeeId)
    {
        if (!await CanAccessAsync(employeeId)) return Forbid();
        return Ok(await _db.EmployeeDocuments.AsNoTracking().Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new { x.Id, x.DocumentType, x.OriginalFileName, x.ContentType, x.FileSize, x.Notes, x.UploadedAtUtc })
            .ToListAsync());
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int employeeId,
        [FromForm] string documentType,
        IFormFile file,
        [FromForm] string? notes)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (!await CanAccessAsync(employeeId)) return Forbid();
        if (!await _db.Employees.AnyAsync(x => x.Id == employeeId)) return NotFound();
        if (!AllowedTypes.Contains(documentType)) return BadRequest(new { message = $"Document type must be one of: {string.Join(", ", AllowedTypes)}." });
        if (file.Length == 0 || file.Length > MaxFileSize) return BadRequest(new { message = "File must be between 1 byte and 10 MB." });
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension)) return BadRequest(new { message = "Only PDF, JPG, JPEG, and PNG files are allowed." });

        Directory.CreateDirectory(_storageRoot);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destination = SafeStoragePath(storedName);
        await using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await file.CopyToAsync(stream);

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId, DocumentType = documentType,
            OriginalFileName = Path.GetFileName(file.FileName), StoredFileName = storedName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length, Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            UploadedByUserAccountId = userId.Value
        };
        _db.EmployeeDocuments.Add(document); await _db.SaveChangesAsync();
        return Created(string.Empty, new { document.Id, document.DocumentType, document.OriginalFileName, document.FileSize, document.UploadedAtUtc });
    }

    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int employeeId, int documentId)
    {
        if (!await CanAccessAsync(employeeId)) return Forbid();
        var document = await _db.EmployeeDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId);
        if (document == null) return NotFound();
        var path = SafeStoragePath(document.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "The stored file is missing." });
        return PhysicalFile(path, document.ContentType, document.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int employeeId, int documentId)
    {
        if (!await CanAccessAsync(employeeId)) return Forbid();
        var document = await _db.EmployeeDocuments.SingleOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId);
        if (document == null) return NotFound();
        _db.EmployeeDocuments.Remove(document); await _db.SaveChangesAsync();
        var path = SafeStoragePath(document.StoredFileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return NoContent();
    }

    private async Task<bool> CanAccessAsync(int employeeId)
    {
        if (User.IsInRole("Admin")) return true;

        var employeeIdClaim = User.FindFirstValue("employee_id");
        if (int.TryParse(employeeIdClaim, out var claimedEmployeeId))
            return claimedEmployeeId == employeeId;

        var userId = GetCurrentUserId();
        return userId.HasValue && await _db.UserAccounts.AnyAsync(x => x.Id == userId && x.EmployeeId == employeeId);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : null;
    }

    private string SafeStoragePath(string storedName)
    {
        var root = Path.GetFullPath(_storageRoot) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(_storageRoot, Path.GetFileName(storedName)));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid document path.");
        return path;
    }
}
