using HRConnect.API.Security;
using HRConnect.API.Options;
using HRConnect.Application.DTOs;
using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Google.Apis.Auth;

namespace HRConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "hrconnect_refresh_token";
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly GoogleAuthOptions _googleAuthOptions;
    private readonly PasswordResetOptions _passwordResetOptions;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IOptions<GoogleAuthOptions> googleAuthOptions,
        IOptions<PasswordResetOptions> passwordResetOptions,
        IPasswordResetEmailSender passwordResetEmailSender,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _googleAuthOptions = googleAuthOptions.Value;
        _passwordResetOptions = passwordResetOptions.Value;
        _passwordResetEmailSender = passwordResetEmailSender;
        _environment = environment;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("google/config")]
    public IActionResult GetGoogleConfig()
    {
        return Ok(new
        {
            enabled = !string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId),
            clientId = _googleAuthOptions.ClientId
        });
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<LoginResponseDto>> GoogleLogin(GoogleLoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "Google login is not configured.");
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                dto.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleAuthOptions.ClientId]
                });

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            {
                return Unauthorized(new { message = "Google did not provide a verified email address." });
            }

            var fullName = string.IsNullOrWhiteSpace(payload.Name)
                ? payload.Email.Split('@')[0]
                : payload.Name;
            var user = await _authService.FindOrCreateExternalUserAsync(
                "Google",
                payload.Subject,
                fullName,
                payload.Email);
            var refreshToken = await _authService.IssueRefreshTokenAsync(user, _jwtOptions.RefreshTokenDays);
            SetRefreshTokenCookie(refreshToken);

            return Ok(CreateLoginResponse(user));
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "The Google sign-in credential is invalid or expired." });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user and returns a signed JWT bearer token.
    /// </summary>
    /// <param name="dto">User login credentials.</param>
    /// <returns>Access token and authenticated user information.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _authService.AuthenticateAsync(dto.Email.Trim(), dto.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var refreshToken = await _authService.IssueRefreshTokenAsync(user, _jwtOptions.RefreshTokenDays);
        SetRefreshTokenCookie(refreshToken);

        return Ok(CreateLoginResponse(user));
    }

    /// <summary>
    /// Creates a new active user account and returns a signed JWT bearer token.
    /// </summary>
    /// <param name="dto">New user account details.</param>
    /// <returns>Access token and authenticated user information.</returns>
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<ActionResult<LoginResponseDto>> Signup(SignupRequestDto dto)
    {
        try
        {
            var user = await _authService.RegisterAsync(dto.FullName, dto.Email, dto.Password);
            var refreshToken = await _authService.IssueRefreshTokenAsync(user, _jwtOptions.RefreshTokenDays);
            SetRefreshTokenCookie(refreshToken);

            return Created(string.Empty, CreateLoginResponse(user));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(
        ForgotPasswordRequestDto dto,
        CancellationToken cancellationToken)
    {
        const string genericMessage = "If an active account uses that email, a password-reset link has been prepared.";
        var issue = await _authService.CreatePasswordResetAsync(
            dto.Email.Trim(),
            _passwordResetOptions.TokenLifetimeMinutes);

        string? developmentResetUrl = null;
        if (issue != null)
        {
            var resetUrl = $"{_passwordResetOptions.FrontendBaseUrl.TrimEnd('/')}/?resetToken={Uri.EscapeDataString(issue.Token)}";
            var delivered = await _passwordResetEmailSender.SendAsync(
                issue.Email,
                issue.FullName,
                resetUrl,
                issue.ExpiresAtUtc,
                cancellationToken);

            if (!delivered && _environment.IsDevelopment())
                developmentResetUrl = resetUrl;
            else if (!delivered)
                _logger.LogError("A password-reset link could not be delivered because email is not configured or delivery failed.");
        }

        return Ok(new ForgotPasswordResponseDto
        {
            Message = genericMessage,
            DevelopmentResetUrl = developmentResetUrl
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
    {
        var succeeded = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        if (!succeeded)
            return BadRequest(new { message = "This password-reset link is invalid, expired, or has already been used." });

        ClearRefreshTokenCookie();
        return Ok(new { message = "Your password has been reset. Sign in with your new password." });
    }

    /// <summary>
    /// Rotates the refresh token cookie and returns a new signed JWT bearer token.
    /// </summary>
    /// <returns>New access token and authenticated user information.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var rotationResult = await _authService.RotateRefreshTokenAsync(
            refreshToken,
            _jwtOptions.RefreshTokenDays);

        if (rotationResult == null)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        }

        SetRefreshTokenCookie(rotationResult.RefreshToken);
        return Ok(CreateLoginResponse(rotationResult.User));
    }

    /// <summary>
    /// Revokes the current refresh token and clears the refresh token cookie.
    /// </summary>
    /// <returns>No content.</returns>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }

        ClearRefreshTokenCookie();
        return NoContent();
    }

    private LoginResponseDto CreateLoginResponse(UserAccount user)
    {
        var (token, expiresAtUtc) = _jwtTokenService.CreateAccessToken(user);

        return new LoginResponseDto
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            User = MapToDto(user)
        };
    }

    private void SetRefreshTokenCookie(RefreshTokenIssue refreshToken)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken.Token,
            CreateRefreshTokenCookieOptions(refreshToken.ExpiresAtUtc));
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshTokenCookieOptions(DateTimeOffset.UnixEpoch));
    }

    private static CookieOptions CreateRefreshTokenCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires,
            Path = "/api/auth"
        };
    }

    private static AuthenticatedUserDto MapToDto(UserAccount user)
    {
        return new AuthenticatedUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            EmployeeId = user.EmployeeId
        };
    }
}
