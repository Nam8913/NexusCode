# Project Context Snapshot

> Auto-generated for AI assistant comprehension. Not a user-facing document.

---

## Project Overview

**Name:** Nexus Code Intelligence Platform

**Purpose:** Roslyn-based code intelligence system for C# and Unity projects. Analyzes code semantically, builds a Knowledge Graph of relationships, and exposes everything via REST API + MCP protocol for AI agents.

**Main Goals:**
- Understand C# code like a compiler (via Roslyn)
- Build Knowledge Graph of code relationships
- Provide symbol search and navigation
- Integrate with AI agents via MCP protocol
- Support Unity-specific patterns (MonoBehaviour, SerializeField, etc.)
- Enable Graph RAG for AI context generation

**Current Stage:** Phase 1-10 Complete. Backend + NexusGraph frontend operational.

---

## High-Level Architecture

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

### Subsystems

**Roslyn Engine**
→ Parses C# source, builds Semantic Model, extracts symbols
→ Dependencies: Microsoft.CodeAnalysis.CSharp
→ Key APIs: `RoslynEngine.AnalyzeFile()`, `SyntaxWalker`

**Knowledge Graph**
→ Stores code relationships as typed nodes and edges in-memory
→ Dependencies: None (pure C#)
→ Key APIs: `KnowledgeGraph.AddNode()`, `AddEdge()`, `GetNodesByKind()`, `GetOutgoingEdges()`

**Symbol Table**
→ Multi-index symbol storage with fast lookup
→ Dependencies: None
→ Key APIs: `SymbolTable.Add()`, `GetByFullName()`, `GetByKind()`, `GetReferences()`

**Symbol Search Engine**
→ Fuzzy symbol search + graph-based caller/callee navigation
→ Dependencies: KnowledgeGraph, SymbolTable
→ Key APIs: `SymbolSearchEngine.FindSymbol()`, `FindCallers()`, `FindCallees()`

**MCP Server**
→ 10 tools exposing code intelligence to AI agents
→ Dependencies: SymbolSearchEngine, KnowledgeGraph
→ Key APIs: `McpServer.HandleRequest()`

**Embedding Engine**
→ Vector embeddings via Ollama for semantic search
→ Dependencies: Ollama HTTP API
→ Key APIs: `EmbeddingEngine.GetEmbeddingAsync()`

**Vector Store**
→ Qdrant + InMemory adapters for vector storage
→ Dependencies: Qdrant HTTP API (optional)
→ Key APIs: `IVectorStore.SearchAsync()`, `UpsertAsync()`

**Context Builder**
→ Graph RAG pipeline: question → symbol search → graph expansion → prompt
→ Dependencies: SymbolSearchEngine, KnowledgeGraph
→ Key APIs: `ContextBuilder.BuildContext()`, `GraphRAGEngine.Answer()`

**REST API**
→ ASP.NET Core endpoints for all operations
→ Dependencies: All backend projects
→ Key APIs: `/api/index/*`, `/api/search/*`, `/api/graph/*`, `/api/rag/*`

**NexusGraph (Frontend)**
→ React + Sigma.js interactive graph visualization
→ Dependencies: NexusCode API (HTTP)
→ Key: Consumes `/api/graph/export`, `/api/search/*`

---

## Solution Structure

```
NexusCode.slnx
├── NexusCode.Domain          # Entities, Enums, Interfaces
├── NexusCode.Roslyn          # Core analysis engine (largest project)
├── NexusCode.Indexer         # CLI indexing tool
├── NexusCode.Mcp             # MCP Server (10 tools)
├── NexusCode.Embedding       # Ollama embedding engine
├── NexusCode.VectorStore     # Qdrant + InMemory adapters
├── NexusCode.Context         # Context Builder for RAG
├── NexusCode.Api             # ASP.NET Core REST API
├── NexusCode.Database        # SQLite persistence
├── NexusCode.Tests           # Unit + Integration tests
├── NexusCode.Graph           # (Reserved)
└── NexusCode.Symbols         # (Reserved)

NexusGraph/                   # Separate React project
├── src/pages/                # Dashboard, Graph, Symbols, Search, RAG, About
├── src/components/           # Layout, GraphCanvas, NodeDetails
├── src/utils/                # API client, colors, graph helpers
└── src/types/                # TypeScript types
```

### Per-Project Details

| Project | Responsibility | Dependencies | Entry Points |
|---------|---------------|--------------|--------------|
| NexusCode.Domain | Entities, Enums, Interfaces | None (leaf) | All entity types |
| NexusCode.Roslyn | Roslyn analysis, SymbolTable, KnowledgeGraph, Search | Domain, Microsoft.CodeAnalysis.CSharp | `RoslynEngine`, `SymbolTable`, `KnowledgeGraph`, `SymbolSearchEngine` |
| NexusCode.Indexer | CLI indexing tool | Domain, Roslyn | `CodeIndexer`, `Program.cs` |
| NexusCode.Mcp | MCP Server (10 tools) | Domain, Roslyn | `McpServer` |
| NexusCode.Embedding | Ollama embedding | Domain | `EmbeddingEngine`, `OllamaClient` |
| NexusCode.VectorStore | Vector DB adapters | Domain | `IVectorStore`, `QdrantAdapter`, `InMemoryVectorStore` |
| NexusCode.Context | Context Builder | Domain, Roslyn | `ContextBuilder`, `GraphRAGEngine` |
| NexusCode.Api | REST API | Domain, Roslyn, Context | `Program.cs`, Controllers |
| NexusCode.Database | SQLite persistence | Domain, Roslyn | `SqliteRepository` |
| NexusCode.Tests | Tests | All projects | Test classes |
| NexusGraph | React frontend | NexusCode API | `src/main.tsx` |

---

## Data Flow

### Indexing Pipeline

```
Repository Path
  → FileScanner (discover .cs files)
  → RoslynEngine (load projects, build compilations)
  → SyntaxWalker (parse each file)
  → SymbolTable (store symbols + references)
  → KnowledgeGraph (store nodes + edges)
  → [Optional] EmbeddingEngine (generate vectors)
```

### Query Pipeline

```
User Query (search/RAG)
  → SymbolSearchEngine.FindSymbol()
  → KnowledgeGraph traversal (callers/callees/implementations)
  → ContextBuilder (aggregate results)
  → Prompt generation
  → [Optional] LLM via Ollama
```

### Graph Visualization Pipeline

```
NexusGraph Frontend
  → GET /api/graph/export
  → JSON response (nodes + edges with colors)
  → Graphology graph construction
  → Sigma.js WebGL rendering
```

---

## Important Types

### Domain Layer

| Type | Responsibility |
|------|---------------|
| `SymbolEntity` | Symbol metadata (name, kind, file, line, access) |
| `GraphNodeEntity` | Graph node (id, label, kind, metadata) |
| `GraphEdgeEntity` | Graph edge (source, target, kind, weight) |
| `ReferenceEntity` | Symbol reference tracking |
| `IndexResult` | Indexing operation result |
| `NodeKind` | 22 node types (Class, Method, Property, etc.) |
| `EdgeKind` | 27 edge types (Calls, Inherits, Implements, etc.) |
| `SymbolKind` | Symbol classification (Type, Method, Field, etc.) |

### Roslyn Layer

| Type | Responsibility |
|------|---------------|
| `RoslynEngine` | Load solutions/projects, build compilations, analyze files |
| `SyntaxWalker` | Visit syntax nodes, extract symbols and references |
| `SymbolTable` | Multi-index concurrent symbol storage |
| `KnowledgeGraph` | In-memory graph with adjacency lists |
| `SymbolSearchEngine` | Fuzzy search + graph traversal |
| `FileScanner` | File discovery with exclusion patterns |
| `IncrementalIndexer` | Checkpoint-based incremental indexing |
| `UnityAnalyzer` | MonoBehaviour/ScriptableObject detection |
| `AssemblyDefinitionAnalyzer` | .asmdef file parsing |
| `MultiRepoManager` | Multi-repository indexing and search |

### API Layer

| Type | Responsibility |
|------|---------------|
| `NexusIndexService` | Shared indexing service for API |
| `GraphController` | Graph endpoints (stats, export, nodes, edges) |
| `SearchController` | Search endpoints (symbol, callers, callees) |
| `IndexController` | Indexing endpoints (repository, status) |
| `RagController` | RAG endpoint (ask question) |
| `MultiRepoController` | Multi-repo endpoints |

### Context Layer

| Type | Responsibility |
|------|---------------|
| `ContextBuilder` | Question → symbol search → graph expansion → context |
| `GraphRAGEngine` | Full RAG pipeline with evidence collection |
| `Chunker` | Code chunking for embedding |

---

## Current Features

| Feature | Status | Main Classes |
|---------|--------|--------------|
| Roslyn Analysis | ✅ Complete | RoslynEngine, SyntaxWalker |
| Knowledge Graph | ✅ Complete | KnowledgeGraph, GraphNodeEntity, GraphEdgeEntity |
| Symbol Search | ✅ Complete | SymbolSearchEngine, SymbolTable |
| MCP Server (10 tools) | ✅ Complete | McpServer |
| Embeddings | ✅ Complete | EmbeddingEngine, OllamaClient |
| Vector Store | ✅ Complete | QdrantAdapter, InMemoryVectorStore |
| Context Builder | ✅ Complete | ContextBuilder |
| Graph RAG | ✅ Complete | GraphRAGEngine |
| Unity Intelligence | ✅ Complete | UnityAnalyzer, AssemblyDefinitionAnalyzer, UnitySceneAnalyzer |
| Multi-Repo | ✅ Complete | MultiRepoManager, CrossRepoSearchEngine, RepoComparator |
| REST API | ✅ Complete | 16 endpoints, Swagger |
| SQLite Database | ✅ Complete | SqliteRepository |
| NexusGraph Frontend | ✅ Complete | React + Sigma.js |
| Unit Tests | ✅ Complete | 42/42 passing |

---

## Planned Features

| Feature | Priority | Dependencies |
|---------|----------|--------------|
| Graph export (PNG/SVG) | High | NexusGraph |
| Responsive design | Medium | NexusGraph |
| Keyboard shortcuts | Medium | NexusGraph |
| Graph layout persistence | Medium | NexusGraph |
| PostgreSQL adapter | Low | Database |
| Multi-language support | Low | Roslyn Engine |
| Collaborative features | Low | API + Auth |

---

## Architecture Decisions

1. **Roslyn over MSBuildWorkspace**: MSBuildLocator incompatible with .NET 10. Parse .csproj manually.
2. **In-Memory Graph**: ConcurrentDictionary with adjacency lists. Fast traversal, SQLite for persistence.
3. **React + Sigma.js**: Blazor had SDK issues. React provides better graph visualization via WebGL.
4. **SQLite over PostgreSQL**: Zero-config, single-file, sufficient for local-first.
5. **Symbol IDs from MD5**: Deterministic GUIDs from fully qualified names for consistency.
6. **Qdrant + InMemory**: InMemory as fallback when Qdrant unavailable.
7. **MCP Protocol**: Standard protocol for AI agent integration.

---

## Known Problems

1. **SQLite vulnerability**: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 has known CVE (moderate)
2. **NPM audit**: esbuild/vite vulnerabilities in NexusGraph (moderate)
3. **Large graphs**: >1M nodes may exceed memory limits
4. **No concurrent writes**: InMemory graph not thread-safe for writes
5. **Limited NuGet resolution**: Simplified .csproj parsing, no full NuGet graph
6. **No incremental graph updates**: Full re-index required for schema changes

---

## Extension Points

- **New NodeKind/EdgeKind**: Add to `NexusCode.Domain/Enums.cs`
- **New MCP Tools**: Add to `NexusCode.Mcp/McpServer.cs`
- **New API Endpoints**: Add controllers in `NexusCode.Api/Controllers/`
- **New Unity Analyzers**: Add to `NexusCode.Roslyn/Unity*.cs`
- **New Vector Store**: Implement `IVectorStore` interface
- **New Frontend Pages**: Add to `NexusGraph/src/pages/`
- **New Graph Export Formats**: Add to `GraphController`

---

## Glossary

| Term | Definition |
|------|-----------|
| **Knowledge Graph** | Directed graph of code relationships (nodes = symbols, edges = relationships) |
| **Symbol** | Named code element (class, method, property, etc.) |
| **Edge** | Typed relationship between symbols (CALLS, INHERITS, etc.) |
| **MCP** | Model Context Protocol - standard for AI agent integration |
| **Graph RAG** | Retrieval-Augmented Generation using graph traversal |
| **SymbolTable** | Multi-index lookup structure for fast symbol search |
| **ContextBuilder** | Pipeline that generates LLM prompts from code context |
| **NexusGraph** | React frontend for interactive graph visualization |

---

## Repository Statistics

| Metric | Value |
|--------|-------|
| Projects | 12 (.NET) + 1 (React) |
| C# Files | 58 |
| Lines of Code | ~8,070 |
| Test Files | 8 |
| Tests | 42/42 passing |
| API Endpoints | 16 |
| MCP Tools | 10 |
| Frontend Pages | 6 |
| NuGet Packages | 4 (xunit, swashbuckle, sqlite, roslyn) |
