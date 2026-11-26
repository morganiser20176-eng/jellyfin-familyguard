namespace Jellyfin.Plugin.FamilyGuard.Models;

/// <summary>A media item waiting for a parental decision.</summary>
public sealed class ReviewItemDto
{
    /// <summary>Gets or sets the Jellyfin identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the title.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the production year.</summary>
    public int? Year { get; set; }

    /// <summary>Gets or sets the official rating.</summary>
    public string? Rating { get; set; }

    /// <summary>Gets or sets the genres.</summary>
    public string[] Genres { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the synopsis.</summary>
    public string? Overview { get; set; }
}

/// <summary>Queue response for one protected profile.</summary>
public sealed class ReviewQueueDto
{
    /// <summary>Gets or sets the selected profile.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets the candidate media.</summary>
    public IReadOnlyList<ReviewItemDto> Items { get; set; } = Array.Empty<ReviewItemDto>();
}

/// <summary>Request for a batch parental decision.</summary>
public sealed class DecisionRequest
{
    /// <summary>Gets or sets the profile name.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets the action: approve or reject.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the target Jellyfin identifiers.</summary>
    public string[] ItemIds { get; set; } = Array.Empty<string>();
}

/// <summary>Internal automation request limited to the configured child profile tags.</summary>
public sealed class AutomationRequest
{
    /// <summary>Gets or sets the profile name.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets one action: approve, review or clear_review.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the target Jellyfin identifiers.</summary>
    public string[] ItemIds { get; set; } = Array.Empty<string>();
}

/// <summary>Requests a read-only preview or an explicit manual classification of recent media.</summary>
public sealed class ScanRequest
{
    /// <summary>Gets or sets the profile whose configured rules are used.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the proposed changes should actually be written.</summary>
    public bool Apply { get; set; }
}

/// <summary>Summarizes a safe, bounded manual classification run.</summary>
public sealed class ScanResultDto
{
    /// <summary>Gets or sets the profile used for the scan.</summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether changes were applied.</summary>
    public bool Applied { get; set; }

    /// <summary>Gets or sets the number of recent unclassified items inspected.</summary>
    public int Considered { get; set; }

    /// <summary>Gets or sets the number of safe matches.</summary>
    public int SafeMatches { get; set; }

    /// <summary>Gets or sets the number of incomplete metadata candidates sent to review.</summary>
    public int ReviewCandidates { get; set; }

    /// <summary>Gets or sets the number of items actually changed.</summary>
    public int Updated { get; set; }
}
