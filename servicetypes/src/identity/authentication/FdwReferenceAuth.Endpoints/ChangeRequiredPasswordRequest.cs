namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// A user changing a password they have been required to change, before they can sign in.
/// </summary>
public sealed class ChangeRequiredPasswordRequest
{
    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the current password — the proof of identity.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password.</summary>
    public string NewPassword { get; set; } = string.Empty;
}
