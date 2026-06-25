# Changelog

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
