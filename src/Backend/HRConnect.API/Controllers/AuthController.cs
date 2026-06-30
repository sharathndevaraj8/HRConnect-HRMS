using HRConnect.API.Security;
using HRConnect.API.Options;
using HRConnect.Application.DTOs;
using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HRConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "hrconnect_refresh_token";
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        IAuthService authService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
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
            Role = user.Role
        };
    }
}
