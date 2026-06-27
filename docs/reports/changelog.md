# Changelog

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
