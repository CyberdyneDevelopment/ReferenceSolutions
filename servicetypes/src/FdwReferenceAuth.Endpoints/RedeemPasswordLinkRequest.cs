namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Someone opening a set-password link and choosing the password (FDW-941).
/// </summary>
public sealed class RedeemPasswordLinkRequest
{
    /// <summary>Gets or sets the token from the link -- the proof that an administrator allowed this.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password.</summary>
    public string NewPassword { get; set; } = string.Empty;
}
