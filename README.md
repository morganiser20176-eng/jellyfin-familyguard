# FamilyGuard for Jellyfin

[Français](README.fr.md) · English

FamilyGuard is a local-first parental review workflow for Jellyfin. It helps an administrator decide which movies and series are available to each protected profile without moving, deleting, uploading, or analysing media files.

## What it does

- Supports multiple protected profiles, each with an allowed tag and a review tag.
- Lets an administrator approve or reject a selection from a review queue.
- Provides conservative, opt-in rules for a manual scan of recently added media.
- Keeps the latest 500 tag decisions in the plugin's local Jellyfin configuration.
- Adds an administrative FamilyGuard entry in Jellyfin.

FamilyGuard complements Jellyfin's native parental controls. For each child account, keep native restrictions enabled and allow only that profile's configured **allowed tag**.

## Privacy and safety

- No cloud account, telemetry, analytics, external API, or remote classifier is used.
- FamilyGuard never transfers media files or library metadata outside the Jellyfin server.
- It never moves, renames, or deletes media files.
- The plugin changes only Jellyfin tags and its own local configuration.
- Decisions require an elevated Jellyfin administrator session.

Read the full [privacy notice](PRIVACY.md) and [security policy](SECURITY.md) before using the plugin for a family profile.

## Installation

Once a public release exists, add the following address in **Dashboard → Plugins → Repositories → Add**:

```text
https://github.com/morganiser20176-eng/jellyfin-familyguard/releases/latest/download/manifest.json
```

Restart Jellyfin after installation when requested, then open **Dashboard → Plugins → FamilyGuard**.

## First configuration

1. Create a FamilyGuard profile and choose its allowed and review tags.
2. In Jellyfin's native parental controls, restrict the child's user account to the allowed tag.
3. Add uncertain media to the review tag, or use **Preview** before applying the conservative manual rules.
4. Approve or reject the review queue as an administrator.

## Compatibility

The first public release targets Jellyfin `10.11.11` and .NET 9. Jellyfin plugins are ABI-sensitive: do not install this release on a server version outside its declared compatibility range.

## Community and feedback

Questions, compatibility reports, and feature ideas are welcome in the
[FamilyGuard Discussions](https://github.com/morganiser20176-eng/jellyfin-familyguard/discussions).
Please include the Jellyfin server version when reporting an issue.

## Development and releases

```bash
dotnet build FamilyGuard.slnx --configuration Release
```

The release workflow packages the plugin and publishes a repository manifest as a GitHub Release asset. Create a release tagged `v0.3.0.0` (or the matching version) to publish it. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

FamilyGuard is distributed under the [GNU GPL v3.0](LICENSE).
