using HRConnect.Domain.Entities;

namespace HRConnect.Application.Models;

public sealed record RefreshTokenRotationResult(UserAccount User, RefreshTokenIssue RefreshToken);
