using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FamilyGuard.Configuration;

/// <summary>Tag mapping for one protected Jellyfin profile.</summary>
public sealed class ProtectedProfile
{
    /// <summary>Gets or sets the display name used in the FamilyGuard dashboard.</summary>
    public string Name { get; set; } = "Child profile";

    /// <summary>Gets or sets the tag which Jellyfin allows for this profile.</summary>
    public string AllowedTag { get; set; } = "FamilyGuard-Allowed";

    /// <summary>Gets or sets the tag used for media pending a parent decision.</summary>
    public string ReviewTag { get; set; } = "FamilyGuard-Review";

    /// <summary>Gets or sets the optional Jellyfin user name associated with this rule set.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable Jellyfin user identifier associated with this profile.
    /// An empty value keeps the profile in review-only mode.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether native Jellyfin tag filtering should be enforced
    /// for the associated user. This setting never changes a user policy until an administrator
    /// explicitly applies the previewed change.
    /// </summary>
    public bool NativeTagProtectionEnabled { get; set; }

    /// <summary>
    /// Gets or sets the allowed tags previously written by FamilyGuard. They are used to make
    /// a later update or removal reversible without replacing unrelated administrator settings.
    /// </summary>
    public List<string> ManagedAllowedTags { get; set; } = [];

    /// <summary>Gets or sets the conservative rules used by the optional manual classifier.</summary>
    public ProfileRules Rules { get; set; } = new();
}

/// <summary>Conservative, opt-in classification rules for one protected profile.</summary>
public sealed class ProfileRules
{
    /// <summary>Gets or sets a value indicating whether manual classification is available.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether safe matches may be approved without a parent click.</summary>
    public bool AutoApproveSafeMatches { get; set; }

    /// <summary>Gets or sets a value indicating whether movies are considered.</summary>
    public bool IncludeMovies { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether series are considered.</summary>
    public bool IncludeSeries { get; set; } = true;

    /// <summary>Gets or sets how many recent days a manual scan is allowed to inspect.</summary>
    public int LookbackDays { get; set; } = 14;

    /// <summary>Gets or sets accepted ratings for a safe automatic match.</summary>
    public List<string> AllowedRatings { get; set; } = ["G", "TV-G", "TV-Y", "TV-Y7", "TP", "FR-TP", "FR-U"];

    /// <summary>
    /// Gets or sets required genre groups. Every group must match; alternatives in one group are separated by a pipe.
    /// Example: <c>Animation</c> and <c>Familial|Family|Children|Kids</c>.
    /// </summary>
    public List<string> RequiredGenreGroups { get; set; } = ["Animation", "Familial|Family|Children|Kids"];

    /// <summary>Gets or sets genres which always prevent a safe match.</summary>
    public List<string> BlockedGenres { get; set; } = ["Action", "Crime", "Horreur", "Horror", "Mystère", "Mystery", "Thriller"];
}

/// <summary>An immutable-looking audit entry written whenever FamilyGuard changes tags.</summary>
public sealed class DecisionLogEntry
{
    /// <summary>Gets or sets the decision timestamp in UTC.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Gets or sets the configured protected profile name.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets the Jellyfin item identifier.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Gets or sets the item title as it was when the change was made.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the action that changed the item.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the change came from a parent, scanner, or local automation.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets a short, non-sensitive explanation.</summary>
    public string Details { get; set; } = string.Empty;
}

/// <summary>Plugin configuration.</summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the protected profiles handled by the plugin.</summary>
    public List<ProtectedProfile> Profiles { get; set; } = [];

    /// <summary>Gets or sets the latest FamilyGuard decisions, newest first.</summary>
    public List<DecisionLogEntry> DecisionLog { get; set; } = [];
}
