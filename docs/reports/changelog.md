# Changelog

## [2.5.0] - Unreleased

### Added
- Added `FilePath` string field to `ReferenceEntity` for storing actual file paths during indexing
- Added fuzzy symbol resolution (`ResolveSymbolByName`) in MCP server — resolves short names (e.g. `PlayerController`) with priority: Type > Method > Property/Field
- Added source code text search to `search_code` MCP tool — now matches both symbol names AND actual source code content (e.g. `public void` returns matching lines with file:line)
- Added `SourceCodeMatch` data class for source text search results in `SymbolSearchEngine`

### Fixed
- **`find_references` returned `00000000-0000-0000-0000-000000000000` instead of file paths** — Root cause: `SyntaxWalker` set `SourceFileId = Guid.Empty` without storing actual path. Fixed by populating `FilePath` field on `ReferenceEntity` during reference creation, and updating MCP handler output to display file paths
- **`get_symbol_info` failed with short names** (e.g. `GameService`) — Root cause: handler used exact `GetByFullName` lookup which requires `global::` prefix. Fixed by adding fuzzy resolution that tries exact match → `global::` prefixed match → name match → contains match
- **`find_implementations` failed with short names** (e.g. `IInventoryView`) — Same root cause and fix as above
- **`find_references`/`get_symbol_info` resolved wrong symbol for `PlayerController`** — Root cause: Property `PlayerController` in `GameService.cs` had FullName `PlayerController` matching the short name, while the Type had `global::PlayerController`. Fixed by collecting all candidates and prioritizing Type > Method > others

### Changed
- `HandleFindReferences`, `HandleFindCallers`, `HandleFindCallees`, `HandleFindImplementations`, `HandleFindDerivedTypes`, `HandleGetSymbolInfo`, `HandleBlastRadius` in MCP server now use `ResolveSymbolByName` instead of direct `GetByFullName`
- `HandleSearchCode` now returns combined results: symbol name matches + source code text matches
- `SyntaxWalker.cs` — `VisitInvocationExpression` and `VisitIdentifierName` now populate `FilePath` on `ReferenceEntity`
- `SymbolSearchEngine.cs` — Added `SearchSourceText()` method with file-based source code grep

### Testing
- Added `SymbolTableResolveTests.cs` (12 tests) — Fuzzy resolution, ReferenceEntity.FilePath regression tests, reference storage with paths
- Added `SearchSourceTextTests.cs` (11 tests) — Source text search with temp files, case insensitive, null/empty queries, max results, score ordering, missing files
- Updated `testing.md` with full 64-test suite documentation, per-test descriptions, edge case coverage, and running instructions
- Test suite: 64/64 passing (was 42/42 in v2.4.0, +22 new tests)

### Files affected
- `src/NexusCode.Domain/Entities.cs` — Added `FilePath` to `ReferenceEntity`
- `src/NexusCode.Roslyn/SyntaxWalker.cs` — Set `FilePath` when creating references
- `src/NexusCode.Roslyn/SymbolSearchEngine.cs` — Added `SearchSourceText()` + `SourceCodeMatch`
- `src/NexusCode.Roslyn/SymbolTable.cs` — Added `ResolveSymbol()` (legacy, superseded by MCP handler logic)
- `src/NexusCode.Mcp/Program.cs` — Added `ResolveSymbolByName()`, updated all handlers
- `src/NexusCode.Tests/SymbolTableResolveTests.cs` — **[NEW]** 12 unit tests
- `src/NexusCode.Tests/SearchSourceTextTests.cs` — **[NEW]** 11 unit tests
- `docs/development/testing.md` — Updated from 42 tests → 64 tests documentation

---

## [2.4.0] - 2026-06-27

### Added
- MCP tool: `blast_radius` - Analyze impact of changing a symbol (callers, callees, references, tests, risk score)
- MCP tool: `pop_symbols` - List symbols filtered by kind and optional name search
- `ColorConfig.cs` - Single source of truth for node/edge colors in Domain layer
- `IndexingService` - Extracted indexing pipeline from NexusIndexService
- `PersistenceService` - Extracted SQLite + config file persistence
- `MultiRepoManagerService` - DI-friendly wrapper for MultiRepoManager
- File locking for config file (`indexed_repos.json`) to prevent race conditions
- Confirmation dialog before deleting repositories

### Changed
- Removed `get_graph_stats` MCP tool (functionality merged into `explain_architecture`)
- MCP server now has 12 tools (was 11 → 13 → settled at 12 due to MiMo limit)
- `NexusIndexService` refactored: constructor injection via DI, implements `IDisposable`
- `MultiRepoController` now uses `MultiRepoManagerService` instead of static singleton
- `GraphController.Export()` uses `ColorConfig` from Domain layer (single source of truth)
- Frontend `colors.ts` kept as display fallback, backend uses `ColorConfig.cs`

### Fixed
- Duplicate color definitions between `colors.ts` and `GraphController.cs` → unified via `ColorConfig.cs`
- Duplicate edge loop in `GraphController.Export()` causing compilation error
- Config file race condition: added `FileStream.Lock/Unlock` for concurrent access
- Silent exception swallowing in `SavePath`/`RemoveSavedPath` → now logs errors
- `IndexController` returns HTTP 500 on failure instead of silent 200
- Dashboard delete now shows confirmation dialog before removing repository
- GraphPage stale closure: `initGraph` now captures current props via refs
- GraphPage memory leak: added `AbortController` and cleanup on unmount

### Removed
- `get_graph_stats` MCP tool (replaced by `explain_architecture`)
- `MultiRepoManagerStatic` static singleton (replaced by DI service)
- Duplicate edge loop in `GraphController.Export()`

---

## [2.3.0] - 2026-06-27

### Added
- MCP tool: `blast_radius` - Analyze impact of changing a symbol
- MCP tool: `pop_symbols` - List symbols filtered by kind and name

### Changed
- Removed `get_graph_stats` (merged into `explain_architecture`)
- MCP server settled at 12 tools (MiMo limit discovered)

---

## [2.2.0] - 2026-06-26

### Added
- Loading spinners for all pages
- Error messages for all API failures (inline)
- Fit-to-screen button on Graph page
- Retry button on Graph error state
- Accessibility labels on inputs

---

## [2.1.0] - 2026-06-26

### Added
- NexusGraph frontend (React + Sigma.js)
- Dashboard, Graph, Symbols, Search, RAG, About pages
- RAG endpoint (`POST /api/rag/ask`)

### Removed
- NexusCode.UI (replaced by NexusGraph)

---

## [2.0.0] - 2026-06-25

### Added
- Unity Intelligence, Multi-Repository Intelligence
- Production Readiness (LRU Cache, Logging, Metrics)
- Streaming Analyzer, Incremental Indexer
- 21 new integration tests

---

## [1.0.0] - 2026-06-25

### Added
- Initial release with Roslyn Analysis, Knowledge Graph, Symbol Search, MCP Server (10 tools), Embedding Engine, Graph RAG, REST API, SQLite Database, 21 unit tests
