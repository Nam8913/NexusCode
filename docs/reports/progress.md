# Progress Log

## 2026-06-27 (v2.4.0)

### Bug Fixes
- Fixed duplicate color definitions → unified via `ColorConfig.cs`
- Fixed config file race condition with `FileStream.Lock`
- Fixed silent exception logging in persistence layer
- Fixed GraphPage stale closure and memory leak
- Fixed Dashboard delete confirmation dialog
- Fixed duplicate edge loop in GraphController

### New Features
- `blast_radius` MCP tool - analyze impact of symbol changes
- `pop_symbols` MCP tool - list symbols by kind/name
- `ColorConfig.cs` - single source of truth for colors
- `IndexingService` - extracted indexing pipeline
- `PersistenceService` - extracted SQLite persistence
- `MultiRepoManagerService` - DI-friendly wrapper

### Removed
- `get_graph_stats` MCP tool (merged into `explain_architecture`)
- `MultiRepoManagerStatic` static singleton

### Metrics
- Files C#: 65+
- Lines code: ~8,500
- Tests: 42/42 passed
- MCP tools: 12
- API endpoints: 16

---

## 2026-06-26 (v2.1.0 - v2.3.0)

### Completed
- NexusGraph frontend (React + Sigma.js)
- Dashboard, Graph, Symbols, Search, RAG, About pages
- Interactive graph visualization with filters
- Added `blast_radius` and `pop_symbols` MCP tools
- Loading spinners, error handling, accessibility
- Fixed duplicate node IDs, graph filters
- Removed NexusCode.UI (replaced by NexusGraph)
