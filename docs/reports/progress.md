# Progress Log

## 2026-06-27

### Completed

- Added `blast_radius` MCP tool (analyze impact of changing a symbol)
- Added `pop_symbols` MCP tool (list symbols by kind/name filter)
- Removed `get_graph_stats` (merged into `explain_architecture`)

### Metrics

- Files C#: 63
- Lines code: ~8,362
- Tests: 42/42 passed
- API endpoints: 16
- MCP tools: 12
- Frontend pages: 6

## 2026-06-26

### Completed

- NexusGraph frontend rewrite
  - React + TypeScript + Sigma.js
  - Dashboard, Graph, Symbols, Search, RAG, About pages
  - Interactive graph visualization with filters
  - Node/edge type filtering
  - Search and highlight
  - Click node for details
- Added RAG endpoint to API
- Fixed duplicate node IDs in graph
- Removed NexusCode.UI (replaced by NexusGraph)
