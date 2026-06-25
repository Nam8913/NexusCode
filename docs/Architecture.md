# Nexus Code Intelligence Platform

## Goal

Build a Roslyn-based Code Intelligence Platform for C# and Unity with Knowledge Graph, Symbol Search, MCP Integration, and Graph RAG.

## Core Modules

1. **Roslyn Analysis Engine** - Syntax/Semantic analysis, Symbol resolution
2. **Knowledge Graph** - Central graph with 15+ edge types
3. **Symbol Search** - Graph-based code navigation
4. **MCP Server** - 10+ tools for AI agent integration
5. **Embedding Engine** - Ollama-based vector generation
6. **Graph RAG** - Combined graph + vector search

## Design Principles

- **Graph First**: Knowledge Graph is the central component
- **Roslyn First**: Deep semantic understanding via Roslyn
- **Symbol First**: Symbol-based navigation and search
- **MCP First**: Native MCP protocol for AI integration
- **Embeddings Second**: Supplementary semantic search
- **Graph RAG Third**: Combined graph + vector for best results

## Architecture Documents

Full architecture documentation is available in `docs/architecture/`:

- [Architecture Index](architecture/INDEX.md) - Complete document listing
- [Requirements](architecture/01-REQUIREMENTS.md) - All requirements
- [Architecture Diagrams](architecture/02-ARCHITECTURE.md) - Mermaid diagrams
- [Domain Model](architecture/03-DOMAIN-MODEL.md) - Entity definitions
- [Roslyn Engine](architecture/04-ROSLYN-ENGINE.md) - Analysis engine
- [Knowledge Graph](architecture/05-KNOWLEDGE-GRAPH.md) - Graph engine
- [Symbol Search](architecture/06-SYMBOL-SEARCH.md) - Search engine
- [Repository Scanner](architecture/07-REPOSITORY-SCANNER.md) - File scanning
- [Unity Intelligence](architecture/08-UNITY-INTELLIGENCE.md) - Unity layer
- [MCP Server](architecture/09-MCP-SERVER.md) - MCP implementation
- [Context Builder](architecture/10-CONTEXT-BUILDER.md) - Context generation
- [Embedding Engine](architecture/11-EMBEDDING-ENGINE.md) - Embedding pipeline
- [Chunking/Qdrant/GraphRAG](architecture/12-CHUNKING-QDRANT-GRAPHRAG.md) - Vector/RAG
- [Agent/Database/Visualization](architecture/15-17-AGENT-DATABASE-VISUALIZATION.md) - Supporting systems
- [Monitoring/Projects/Roadmap](architecture/18-20-MONITORING-PROJECTS-ROADMAP.md) - Operations
- [Audit](architecture/99-AUDIT.md) - Architecture review

## Technology Stack

- **Backend**: .NET 10, C#, ASP.NET Core
- **Analysis**: Roslyn
- **Storage**: PostgreSQL, SQLite
- **Vector DB**: Qdrant, LanceDB
- **AI**: Ollama (nomic-embed-text, mxbai-embed-large, bge-m3)
- **Frontend**: Blazor
- **Graph**: Custom Graph Engine

## Development Roadmap

| Phase | Duration | Focus |
|-------|----------|-------|
| 1 | Weeks 1-3 | Roslyn Indexer |
| 2 | Weeks 4-6 | Knowledge Graph |
| 3 | Weeks 7-9 | Symbol Search |
| 4 | Weeks 10-12 | MCP Server |
| 5 | Weeks 13-15 | Embeddings + Qdrant |
| 6 | Weeks 16-18 | Graph RAG |
| 7 | Weeks 19-21 | Unity Intelligence |
| 8 | Weeks 22-24 | Multi-Repo Intelligence |
