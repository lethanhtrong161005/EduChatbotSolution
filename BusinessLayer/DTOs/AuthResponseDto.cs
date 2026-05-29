using System;
using System.Collections.Generic;

namespace BusinessLayer.DTOs;

/// <summary>
/// Data Transfer Object for authentication responses.
/// Contains user information and status after successful authentication.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Indicates whether the authentication was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The message describing the result of the authentication attempt.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The full name of the authenticated user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the authenticated user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The list of roles assigned to the authenticated user.
    /// </summary>
    public List<string> Roles { get; set; } = new List<string>();
}
