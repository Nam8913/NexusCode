# Changelog

## [2.3.0] - 2026-06-27

### Added
- MCP tool: `blast_radius` - Analyze impact of changing a symbol (callers, callees, references, tests)
- MCP tool: `pop_symbols` - List symbols filtered by kind and optional name search

### Changed
- Removed `get_graph_stats` (functionality merged into `explain_architecture`)
- MCP server now has 12 tools (was 11, then 13, settled at 12)

## [2.2.0] - 2026-06-26

### Added
- Loading spinners for all pages (Dashboard, Symbols, Search, RAG)
- Error messages for all API failures (inline, not alert)
- Fit-to-screen button on Graph page
- Retry button on Graph error state
- Accessibility labels (aria-label) on inputs

### Changed
- Dashboard shows loading state when fetching initial status
- Graph page shows error state with retry option
- RAG page shows inline errors instead of browser alert

## [2.1.0] - 2026-06-26

### Added
- NexusGraph frontend (React + Sigma.js)
- Dashboard page with repository indexing
- Graph visualization with interactive filters
- Symbol browser with callers/callees
- Search page with symbol lookup
- Graph RAG page for AI context
- About page with project info
- RAG endpoint (`POST /api/rag/ask`)

### Changed
- Removed NexusCode.UI (replaced by NexusGraph)
- Graph export endpoint now includes node colors and sizes

### Fixed
- Duplicate node IDs in graph visualization
- Graph filters not working correctly

## [2.0.0] - 2026-06-25

### Added
- Unity Intelligence (Assembly Definitions, Scenes, Prefabs, Addressables)
- Multi-Repository Intelligence (Cross-repo search, comparison, health)
- Production Readiness (LRU Cache, Logging, Metrics, Health Checks)
- Streaming Analyzer (IAsyncEnumerable)
- Incremental Indexer (checkpoint/resume)
- Integration Tests (21 new tests)

### Changed
- Increased test count from 21 to 42

## [1.0.0] - 2026-06-25

### Added
- Initial release
- Roslyn Analysis Engine
- Knowledge Graph
- Symbol Search
- MCP Server (10 tools)
- Embedding Engine
- Graph RAG
- REST API
- SQLite Database
- Unit Tests (21)
