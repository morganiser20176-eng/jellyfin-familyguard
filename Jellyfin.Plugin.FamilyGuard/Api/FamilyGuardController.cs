using Jellyfin.Data.Enums;
using Jellyfin.Plugin.FamilyGuard.Configuration;
using Jellyfin.Plugin.FamilyGuard.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.FamilyGuard.Api;

/// <summary>Administrative endpoints for the FamilyGuard review queue.</summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("FamilyGuard")]
public sealed class FamilyGuardController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private static readonly object ConfigurationLock = new();

    /// <summary>Initializes a new instance of the <see cref="FamilyGuardController"/> class.</summary>
    public FamilyGuardController(ILibraryManager libraryManager, IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
    }

    /// <summary>Gets the review queue for a protected profile.</summary>
    [HttpGet("Queue")]
    public ActionResult<ReviewQueueDto> GetQueue([FromQuery] string? profile = null)
    {
        var selectedProfile = FindProfile(profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
                IsVirtualItem = false,
                Recursive = true
            })
            .Where(item => HasTag(item, selectedProfile.ReviewTag))
            .OrderByDescending(item => item.DateCreated)
            .ThenBy(item => item.SortName, StringComparer.CurrentCultureIgnoreCase)
            .Select(ToDto)
            .ToList();

        return Ok(new ReviewQueueDto { Profile = selectedProfile.Name, Items = items });
    }

    /// <summary>Approves or rejects several candidates at once.</summary>
    [HttpPost("Decisions")]
    public async Task<ActionResult> ApplyDecision([FromBody] DecisionRequest request, CancellationToken cancellationToken)
    {
        var selectedProfile = FindProfile(request.Profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        var action = request.Action.Trim().ToLowerInvariant();
        if (action is not "approve" and not "reject")
        {
            return BadRequest("Invalid action. Use approve or reject.");
        }

        var updated = 0;
        var journal = new List<DecisionLogEntry>();
        foreach (var rawId in request.ItemIds.Distinct(StringComparer.Ordinal))
        {
            if (!Guid.TryParse(rawId, out var itemId))
            {
                continue;
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item is null || !HasTag(item, selectedProfile.ReviewTag))
            {
                continue;
            }

            var tags = (item.Tags ?? Array.Empty<string>()).ToList();
            tags.RemoveAll(tag => string.Equals(tag, selectedProfile.ReviewTag, StringComparison.OrdinalIgnoreCase));
            if (action == "approve" && !tags.Any(tag => string.Equals(tag, selectedProfile.AllowedTag, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(selectedProfile.AllowedTag);
            }

            item.Tags = tags.ToArray();
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            updated++;
            journal.Add(NewJournalEntry(selectedProfile, item, action == "approve" ? "approved" : "rejected", "Parent", "Parent decision made in FamilyGuard."));
        }

        WriteJournal(journal);

        return Ok(new { updated });
    }

    /// <summary>Applies a constrained tag action for the local classifier.</summary>
    [HttpPost("Automation")]
    public async Task<ActionResult> ApplyAutomation([FromBody] AutomationRequest request, CancellationToken cancellationToken)
    {
        var selectedProfile = FindProfile(request.Profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        var action = request.Action.Trim().ToLowerInvariant();
        if (action is not "approve" and not "review" and not "clear_review")
        {
            return BadRequest("Invalid automation action.");
        }

        var updated = 0;
        var journal = new List<DecisionLogEntry>();
        foreach (var rawId in request.ItemIds.Distinct(StringComparer.Ordinal))
        {
            if (!Guid.TryParse(rawId, out var itemId))
            {
                continue;
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item is null)
            {
                continue;
            }

            var tags = (item.Tags ?? Array.Empty<string>()).ToList();
            if (action == "approve")
            {
                tags.RemoveAll(tag => string.Equals(tag, selectedProfile.ReviewTag, StringComparison.OrdinalIgnoreCase));
                if (!tags.Any(tag => string.Equals(tag, selectedProfile.AllowedTag, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(selectedProfile.AllowedTag);
                }
            }
            else if (action == "review")
            {
                if (!tags.Any(tag => string.Equals(tag, selectedProfile.ReviewTag, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(selectedProfile.ReviewTag);
                }
            }
            else
            {
                tags.RemoveAll(tag => string.Equals(tag, selectedProfile.ReviewTag, StringComparison.OrdinalIgnoreCase));
            }

            if (item.Tags?.SequenceEqual(tags, StringComparer.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            item.Tags = tags.ToArray();
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            updated++;
            journal.Add(NewJournalEntry(selectedProfile, item, action, "Automation", "Bounded action from a local classifier."));
        }

        WriteJournal(journal);

        return Ok(new { updated });
    }

    /// <summary>Returns the latest tag decisions for an administrator audit.</summary>
    [HttpGet("Journal")]
    public ActionResult<IReadOnlyList<DecisionLogEntry>> GetJournal([FromQuery] string? profile = null, [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 200);
        var entries = Plugin.Instance?.Configuration.DecisionLog ?? [];
        var result = entries
            .Where(entry => string.IsNullOrWhiteSpace(profile) || string.Equals(entry.Profile, profile, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.CreatedUtc)
            .Take(take)
            .ToList();
        return Ok(result);
    }

    /// <summary>Lists local Jellyfin users that can be linked to a protected profile.</summary>
    [HttpGet("Users")]
    public ActionResult<IReadOnlyList<JellyfinUserDto>> GetUsers()
    {
        var users = _userManager.GetUsers()
            .Select(user =>
            {
                var policy = _userManager.GetUserDto(user).Policy;
                return new JellyfinUserDto
                {
                    Id = user.Id.ToString(),
                    Name = user.Username,
                    IsAdministrator = policy.IsAdministrator,
                    IsDisabled = policy.IsDisabled
                };
            })
            .OrderBy(user => user.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return Ok(users);
    }

    /// <summary>Previews the native Jellyfin allowed-tag policy that FamilyGuard would apply.</summary>
    [HttpGet("NativePolicy/Preview")]
    public ActionResult<NativePolicyPreviewDto> PreviewNativePolicy([FromQuery] string? profile = null)
    {
        var selectedProfile = FindProfile(profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        return Ok(BuildNativePolicyPreview(selectedProfile, restore: false));
    }

    /// <summary>Applies the previewed native Jellyfin allowed-tag policy for a protected profile.</summary>
    [HttpPost("NativePolicy/Apply")]
    public Task<ActionResult<NativePolicyPreviewDto>> ApplyNativePolicy([FromBody] NativePolicyRequest request)
        => ChangeNativePolicy(request, restore: false);

    /// <summary>Removes only the allowed tags previously managed by FamilyGuard from a linked user.</summary>
    [HttpPost("NativePolicy/Restore")]
    public Task<ActionResult<NativePolicyPreviewDto>> RestoreNativePolicy([FromBody] NativePolicyRequest request)
        => ChangeNativePolicy(request, restore: true);

    /// <summary>Previews or explicitly applies conservative rules to recently added, unclassified media.</summary>
    [HttpPost("Scan")]
    public async Task<ActionResult<ScanResultDto>> ScanRecent([FromBody] ScanRequest request, CancellationToken cancellationToken)
    {
        var selectedProfile = FindProfile(request.Profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        var rules = selectedProfile.Rules ?? new ProfileRules();
        if (!rules.Enabled)
        {
            return BadRequest("Rules are disabled for this profile.");
        }

        var lookbackDays = Math.Clamp(rules.LookbackDays, 1, 90);
        var since = DateTime.UtcNow.AddDays(-lookbackDays);
        var result = new ScanResultDto { Profile = selectedProfile.Name, Applied = request.Apply };
        var journal = new List<DecisionLogEntry>();
        var recentItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
                IsVirtualItem = false,
                Recursive = true
            })
            .Where(item => item.DateCreated.ToUniversalTime() >= since)
            .Where(item => SupportsType(item, rules))
            .Where(item => !HasTag(item, selectedProfile.AllowedTag) && !HasTag(item, selectedProfile.ReviewTag))
            .OrderByDescending(item => item.DateCreated)
            .Take(200)
            .ToList();

        foreach (var item in recentItems)
        {
            result.Considered++;
            if (HasBlockedGenre(item, rules))
            {
                continue;
            }

            var safe = HasAllowedRating(item, rules) && HasRequiredGenres(item, rules);
            var incomplete = string.IsNullOrWhiteSpace(item.OfficialRating) || (item.Genres?.Length ?? 0) == 0;
            if (safe)
            {
                result.SafeMatches++;
                if (request.Apply)
                {
                    var action = rules.AutoApproveSafeMatches ? "approve" : "review";
                    if (await ApplyTagAction(item, selectedProfile, action, cancellationToken).ConfigureAwait(false))
                    {
                        result.Updated++;
                        journal.Add(NewJournalEntry(selectedProfile, item, rules.AutoApproveSafeMatches ? "auto_approved" : "queued_safe_match", "ManualScan", rules.AutoApproveSafeMatches ? "Safe rules matched." : "Safe match awaiting a parent decision."));
                    }
                }

                continue;
            }

            if (incomplete)
            {
                result.ReviewCandidates++;
                if (request.Apply && await ApplyTagAction(item, selectedProfile, "review", cancellationToken).ConfigureAwait(false))
                {
                    result.Updated++;
                    journal.Add(NewJournalEntry(selectedProfile, item, "queued_incomplete_metadata", "ManualScan", "Metadata is incomplete: a parent decision is required."));
                }
            }
        }

        if (request.Apply)
        {
            WriteJournal(journal);
        }

        return Ok(result);
    }

    private static bool HasTag(BaseItem item, string tag)
    {
        return item.Tags?.Any(current => string.Equals(current, tag, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool SupportsType(BaseItem item, ProfileRules rules)
    {
        var kind = item.GetBaseItemKind();
        return (kind == BaseItemKind.Movie && rules.IncludeMovies) || (kind == BaseItemKind.Series && rules.IncludeSeries);
    }

    private static bool HasAllowedRating(BaseItem item, ProfileRules rules)
    {
        return rules.AllowedRatings.Count > 0
            && !string.IsNullOrWhiteSpace(item.OfficialRating)
            && rules.AllowedRatings.Any(rating => string.Equals(rating.Trim(), item.OfficialRating, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasRequiredGenres(BaseItem item, ProfileRules rules)
    {
        var genres = item.Genres ?? Array.Empty<string>();
        return rules.RequiredGenreGroups.Count > 0 && rules.RequiredGenreGroups.All(group => group
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(required => genres.Any(genre => string.Equals(genre, required, StringComparison.OrdinalIgnoreCase))));
    }

    private static bool HasBlockedGenre(BaseItem item, ProfileRules rules)
    {
        var genres = item.Genres ?? Array.Empty<string>();
        return genres.Any(genre => rules.BlockedGenres.Any(blocked => string.Equals(genre, blocked.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task<bool> ApplyTagAction(BaseItem item, ProtectedProfile profile, string action, CancellationToken cancellationToken)
    {
        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        if (action == "approve")
        {
            tags.RemoveAll(tag => string.Equals(tag, profile.ReviewTag, StringComparison.OrdinalIgnoreCase));
            if (!tags.Any(tag => string.Equals(tag, profile.AllowedTag, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(profile.AllowedTag);
            }
        }
        else
        {
            if (!tags.Any(tag => string.Equals(tag, profile.ReviewTag, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(profile.ReviewTag);
            }
        }

        if (item.Tags?.SequenceEqual(tags, StringComparer.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        item.Tags = tags.ToArray();
        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static DecisionLogEntry NewJournalEntry(ProtectedProfile profile, BaseItem item, string action, string source, string details)
    {
        return new DecisionLogEntry
        {
            CreatedUtc = DateTimeOffset.UtcNow,
            Profile = profile.Name,
            ItemId = item.Id.ToString(),
            ItemName = item.Name,
            Action = action,
            Source = source,
            Details = details
        };
    }

    private static void WriteJournal(IEnumerable<DecisionLogEntry> entries)
    {
        var pending = entries.ToList();
        var plugin = Plugin.Instance;
        if (pending.Count == 0 || plugin is null)
        {
            return;
        }

        lock (ConfigurationLock)
        {
            plugin.Configuration.DecisionLog ??= [];
            plugin.Configuration.DecisionLog.InsertRange(0, pending.OrderByDescending(entry => entry.CreatedUtc));
            if (plugin.Configuration.DecisionLog.Count > 500)
            {
                plugin.Configuration.DecisionLog.RemoveRange(500, plugin.Configuration.DecisionLog.Count - 500);
            }

            plugin.SaveConfiguration(plugin.Configuration);
        }
    }

    private async Task<ActionResult<NativePolicyPreviewDto>> ChangeNativePolicy(NativePolicyRequest request, bool restore)
    {
        if (!request.Confirm)
        {
            return BadRequest("Explicit confirmation is required before changing a Jellyfin user policy.");
        }

        var selectedProfile = FindProfile(request.Profile);
        if (selectedProfile is null)
        {
            return NotFound("FamilyGuard profile not found.");
        }

        var preview = BuildNativePolicyPreview(selectedProfile, restore);
        if (!preview.CanApply)
        {
            return BadRequest(preview.Warning);
        }

        var user = _userManager.GetUserById(Guid.Parse(preview.UserId));
        if (user is null)
        {
            return NotFound("The linked Jellyfin user no longer exists.");
        }

        var policy = _userManager.GetUserDto(user).Policy;
        policy.AllowedTags = preview.DesiredAllowedTags;
        await _userManager.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(false);

        selectedProfile.UserName = user.Username;
        selectedProfile.ManagedAllowedTags = restore ? [] : [selectedProfile.AllowedTag];
        selectedProfile.NativeTagProtectionEnabled = !restore;
        SaveProfileConfiguration();
        WriteJournal(
        [
            new DecisionLogEntry
            {
                CreatedUtc = DateTimeOffset.UtcNow,
                Profile = selectedProfile.Name,
                ItemId = user.Id.ToString(),
                ItemName = user.Username,
                Action = restore ? "native_policy_restored" : "native_policy_applied",
                Source = "NativePolicy",
                Details = restore
                    ? "Only FamilyGuard-managed allowed tags were removed from the Jellyfin policy."
                    : "Jellyfin allowed-tag policy updated after an administrator preview."
            }
        ]);

        return Ok(BuildNativePolicyPreview(selectedProfile, restore));
    }

    private NativePolicyPreviewDto BuildNativePolicyPreview(ProtectedProfile profile, bool restore)
    {
        var preview = new NativePolicyPreviewDto { Profile = profile.Name, UserId = profile.UserId };
        if (!Guid.TryParse(profile.UserId, out var userId))
        {
            preview.Warning = "Link a Jellyfin user before enabling native protection.";
            return preview;
        }

        if (!restore && !profile.NativeTagProtectionEnabled)
        {
            preview.Warning = "Enable native tag protection in the FamilyGuard profile, save the configuration, then review this preview again.";
            return preview;
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            preview.Warning = "The linked Jellyfin user could not be found.";
            return preview;
        }

        var userPolicy = _userManager.GetUserDto(user).Policy;
        preview.UserName = user.Username;
        if (userPolicy.IsAdministrator)
        {
            preview.Warning = "An administrator account cannot be used as a protected FamilyGuard profile.";
            return preview;
        }

        if (userPolicy.IsDisabled)
        {
            preview.Warning = "The linked Jellyfin user is disabled.";
            return preview;
        }

        var conflict = (Plugin.Instance?.Configuration.Profiles ?? [])
            .Any(other => !ReferenceEquals(other, profile)
                && string.Equals(other.UserId, profile.UserId, StringComparison.OrdinalIgnoreCase)
                && other.NativeTagProtectionEnabled);
        if (conflict)
        {
            preview.Warning = "This Jellyfin user is already protected by another active FamilyGuard profile.";
            return preview;
        }

        var current = NormalizeTags(userPolicy.AllowedTags);
        var managed = NormalizeTags(profile.ManagedAllowedTags);
        var desired = current.Where(tag => !managed.Contains(tag, StringComparer.OrdinalIgnoreCase));
        if (!restore)
        {
            desired = desired.Append(profile.AllowedTag.Trim());
        }

        preview.CurrentAllowedTags = current;
        preview.DesiredAllowedTags = NormalizeTags(desired);
        preview.AddedAllowedTags = preview.DesiredAllowedTags
            .Where(tag => !current.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        preview.RemovedAllowedTags = current
            .Where(tag => !preview.DesiredAllowedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        preview.CanApply = true;
        return preview;
    }

    private static string[] NormalizeTags(IEnumerable<string>? tags)
    {
        return (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void SaveProfileConfiguration()
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return;
        }

        lock (ConfigurationLock)
        {
            plugin.SaveConfiguration(plugin.Configuration);
        }
    }

    private static ReviewItemDto ToDto(BaseItem item)
    {
        return new ReviewItemDto
        {
            Id = item.Id.ToString(),
            Name = item.Name,
            Type = item.GetBaseItemKind().ToString(),
            Year = item.ProductionYear,
            Rating = item.OfficialRating,
            Genres = item.Genres ?? Array.Empty<string>(),
            Overview = item.Overview
        };
    }

    private static ProtectedProfile? FindProfile(string? profileName)
    {
        var profiles = Plugin.Instance?.Configuration.Profiles ?? [];
        if (profiles.Count == 0) return null;

        var wantedName = profileName ?? profiles[0].Name;
        return profiles.FirstOrDefault(profile => string.Equals(profile.Name, wantedName, StringComparison.OrdinalIgnoreCase));
    }
}
