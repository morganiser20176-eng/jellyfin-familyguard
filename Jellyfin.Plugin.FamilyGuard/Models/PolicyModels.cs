namespace Jellyfin.Plugin.FamilyGuard.Models;

/// <summary>A non-sensitive Jellyfin user exposed to the FamilyGuard administrator.</summary>
public sealed class JellyfinUserDto
{
    /// <summary>Gets or sets the stable user identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the account is an administrator.</summary>
    public bool IsAdministrator { get; set; }

    /// <summary>Gets or sets whether the account is disabled.</summary>
    public bool IsDisabled { get; set; }
}

/// <summary>Read-only impact report for a native Jellyfin user-policy update.</summary>
public sealed class NativePolicyPreviewDto
{
    /// <summary>Gets or sets the FamilyGuard profile name.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected Jellyfin user identifier.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected Jellyfin user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the change can be applied safely.</summary>
    public bool CanApply { get; set; }

    /// <summary>Gets or sets a human-readable reason when the operation cannot be applied.</summary>
    public string? Warning { get; set; }

    /// <summary>Gets or sets the allowed tags currently configured in Jellyfin.</summary>
    public string[] CurrentAllowedTags { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the allowed tags that FamilyGuard would write.</summary>
    public string[] DesiredAllowedTags { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the tags that would be added.</summary>
    public string[] AddedAllowedTags { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the FamilyGuard-managed tags that would be removed.</summary>
    public string[] RemovedAllowedTags { get; set; } = Array.Empty<string>();
}

/// <summary>Explicit request to apply or restore a FamilyGuard-managed native policy.</summary>
public sealed class NativePolicyRequest
{
    /// <summary>Gets or sets the FamilyGuard profile name.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets confirmation that the administrator reviewed the preview.</summary>
    public bool Confirm { get; set; }
}
