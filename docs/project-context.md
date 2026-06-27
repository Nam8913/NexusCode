# Project Context Snapshot

> Auto-generated for AI assistant comprehension.

## Project Overview

**Name:** Nexus Code Intelligence Platform

**Purpose:** Roslyn-based code intelligence for C# and Unity with Knowledge Graph, Symbol Search, MCP integration, and Graph RAG.

**Status:** Production-ready. 12 projects, 65+ source files, 8,500+ lines, 42 tests passing, 12 MCP tools operational.

---

## Recent Changes (v2.4.0)

### Bug Fixes
- Duplicate color definitions unified via `ColorConfig.cs`
- Config file race condition fixed with `FileStream.Lock`
- Silent exception logging added to persistence layer
- GraphPage stale closure and memory leak fixed
- Dashboard delete confirmation added

### New Features
- `blast_radius` MCP tool - analyze impact of symbol changes
- `pop_symbols` MCP tool - list symbols by kind/name
- `ColorConfig.cs` - single source of truth for colors
- `IndexingService` - extracted indexing pipeline
- `PersistenceService` - extracted SQLite persistence
- `MultiRepoManagerService` - DI-friendly wrapper

### Removed
- `get_graph_stats` tool (merged into `explain_architecture`)
- `MultiRepoManagerStatic` static singleton (replaced by DI)

---

## High-Level Architecture

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

| Subsystem | Responsibility | Dependencies |
|-----------|---------------|--------------|
| Roslyn Engine | Parse C#, extract symbols, build semantic model | Microsoft.CodeAnalysis.CSharp |
| Knowledge Graph | Store code relationships, BFS/DFS traversal | None |
| Symbol Table | Multi-index symbol lookup | None |
| Symbol Search | Fuzzy search + graph navigation | KnowledgeGraph, SymbolTable |
| MCP Server | 12 tools for AI agents | SymbolSearchEngine, KnowledgeGraph |
| Embedding Engine | Ollama vector embeddings | Ollama HTTP API |
| Vector Store | Qdrant + InMemory adapters | Qdrant HTTP API |
| Context Builder | Graph RAG pipeline | SymbolSearchEngine, KnowledgeGraph |
| REST API | 16 endpoints | All backend projects |
| NexusGraph | React + Sigma.js visualization | NexusCode API (HTTP) |

---

## Solution Structure

| Project | Files | Lines | Purpose |
|---------|------:|------:|---------|
| NexusCode.Roslyn | 27 | 4,271 | Core analysis engine |
| NexusCode.Tests | 8 | 746 | 42 unit + integration tests |
| NexusCode.Mcp | 3 | 701 | MCP Server (12 tools) |
| NexusCode.Api | 9 | 674 | REST API (16 endpoints) |
| NexusCode.Domain | 4 | 498 | Entities, Enums, Interfaces |
| NexusCode.Embedding | 4 | 448 | Ollama embedding engine |
| NexusCode.Indexer | 3 | 422 | CLI indexing tool |
| NexusCode.Database | 1 | 205 | SQLite persistence |
| NexusCode.VectorStore | 3 | 254 | Qdrant + InMemory |
| NexusCode.Context | 1 | 143 | Context Builder |
| **Total** | **63** | **8,362** | |

---

## Data Flow

### Indexing
Repository → FileScanner → RoslynEngine → SyntaxWalker → SymbolTable + KnowledgeGraph → SQLite

### Query
Question → SymbolSearchEngine → GraphExpansion → ContextBuilder → Prompt

### MCP
AI Agent → JSON-RPC → McpServer → Tools → Results

---

## Key Types

| Type | File | Purpose |
|------|------|---------|
| SymbolTable | Roslyn/SymbolTable.cs | Multi-index symbol storage |
| KnowledgeGraph | Roslyn/KnowledgeGraph.cs | In-memory graph with adjacency lists |
| SymbolSearchEngine | Roslyn/SymbolSearchEngine.cs | Fuzzy search + graph traversal |
| HybridSearchEngine | Roslyn/HybridSearchEngine.cs | RRF fusion search |
| RoslynEngine | Roslyn/RoslynEngine.cs | C# analysis via Roslyn |
| SyntaxWalker | Roslyn/SyntaxWalker.cs | Symbol extraction from syntax trees |
| McpServer | Mcp/Program.cs | 12 MCP tools over stdio |
| ContextBuilder | Context/ContextBuilder.cs | Graph RAG pipeline |
| IncrementalEmbeddingEngine | Embedding/IncrementalEmbeddingEngine.cs | Delta embedding with SQLite |
| CodeIndexer | Indexer/CodeIndexer.cs | CLI indexing pipeline |

---

## MCP Tools (12)

| Tool | Description |
|------|-------------|
| index_repository | Index a C# repository |
| find_symbol | Find symbol by name |
| find_references | Find all references |
| find_callers | Find methods calling a method |
| find_callees | Find methods called by a method |
| find_implementations | Find interface implementations |
| find_derived_types | Find derived types |
| search_code | Search code by query |
| get_symbol_info | Get detailed symbol info |
| explain_architecture | Explain codebase architecture |
| blast_radius | Analyze impact of changing a symbol |
| pop_symbols | List symbols by kind and name filter |

---

## Known Issues

1. SQLitePCLRaw transitive dependency vulnerability (moderate)
2. npm audit warnings in NexusGraph (esbuild/vite)
3. Large graphs (>1M nodes) may exceed memory
4. No concurrent write safety for graph updates from multiple sources

---

## Repository Statistics

| Metric | Value |
|--------|-------|
| Projects | 12 (.NET) + 1 (React) |
| Source files | 65+ |
| Lines of code | ~8,500 |
| Test methods | 42 |
| API endpoints | 16 |
| MCP tools | 12 |
| Frontend pages | 6 |
| Documentation files | 45 |
