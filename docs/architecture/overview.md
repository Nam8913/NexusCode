# Architecture Overview

## System Context

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

## Core Components

| Component | Project | Purpose |
|-----------|---------|---------|
| Domain | NexusCode.Domain | Entities, Enums, Interfaces |
| Roslyn Engine | NexusCode.Roslyn | Code analysis, symbol extraction |
| Knowledge Graph | NexusCode.Roslyn | Graph storage and traversal |
| Symbol Search | NexusCode.Roslyn | Symbol lookup, callers/callees |
| Indexer | NexusCode.Indexer | CLI indexing tool |
| MCP Server | NexusCode.Mcp | 10 tools for AI agents |
| Embedding Engine | NexusCode.Embedding | Ollama integration |
| Vector Store | NexusCode.VectorStore | Qdrant + InMemory adapters |
| Context Builder | NexusCode.Context | Graph RAG pipeline |
| API | NexusCode.Api | REST endpoints |
| Database | NexusCode.Database | SQLite persistence |
| Frontend | NexusGraph | React + Sigma.js visualization |

## Design Principles

1. **Graph First**: Knowledge Graph is the central component
2. **Roslyn First**: Deep semantic understanding via Roslyn
3. **Symbol First**: Symbol-based navigation and search
4. **MCP First**: Native MCP protocol for AI integration
5. **Embeddings Second**: Supplementary semantic search
6. **Graph RAG Third**: Combined graph + vector for best results
