# Contributing

Thanks for helping improve FamilyGuard.

1. Open an issue before a large feature or behavioural change.
2. Keep the plugin local-first: no data collection, cloud dependency, or remote media analysis without an explicit, reviewed design.
3. Preserve the rule that media files are never moved, renamed, deleted, or uploaded by FamilyGuard.
4. Keep English and French documentation in sync. User-facing additions should provide both languages whenever practical.
5. Build the project before opening a pull request:

   ```bash
   dotnet build FamilyGuard.slnx --configuration Release
   ```

6. Do not commit built binaries, personal Jellyfin configuration, API keys, screenshots containing private media, or family-profile names.

By contributing, you agree that your contribution is distributed under GPL-3.0-or-later.
