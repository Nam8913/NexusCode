# Known Issues

## Current

1. **SQLite package vulnerability**: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 has known vulnerability
   - Severity: Moderate
   - Mitigation: Update package when available

2. **NexusGraph npm audit**: 2 vulnerabilities (esbuild, vite)
   - Severity: Moderate
   - Mitigation: Run `npm audit fix --force` (breaking changes)

## Resolved

- ~~Blazor UI build issues~~ → Replaced with NexusGraph
- ~~Duplicate node IDs in graph~~ → Fixed with Set deduplication
